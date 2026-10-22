using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que procesa las notificaciones de la pasarela (spec 017, research §9).
/// Su responsabilidad es seguir los pasos de la confirmación: firma, intento por referencia, bloqueo
/// del cobro, repetición, valor, estado aprobado, cobro anulado, pago duplicado y, si todo cuadra,
/// crear el pago y marcar el cobro como pagado reevaluando la suspensión en la misma transacción.
/// Cada notificación queda registrada con su resultado, sin su cuerpo ni su firma. Un cobro pagado nunca
/// vuelve a pendiente.
/// No confía en el retorno del navegador y nunca registra el cuerpo ni los secretos.
/// </summary>
public class ServicioNotificacionPasarela : IServicioNotificacionPasarela
{
    private const string EstadoAprobado = "APPROVED";

    private readonly IPasarelaDePago _pasarela;
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IServicioSuspensionPorImpago _suspension;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioNotificacionPasarela(
        IPasarelaDePago pasarela,
        IRepositorioCobrosPlataforma cobros,
        IServicioSuspensionPorImpago suspension,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _pasarela = pasarela;
        _cobros = cobros;
        _suspension = suspension;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<ResultadoNotificacion> RecibirAsync(
        string cuerpo, string? checksumDeLaCabecera, CancellationToken cancelacion = default)
    {
        var verificada = _pasarela.VerificarNotificacion(cuerpo, checksumDeLaCabecera);
        if (verificada is null)
        {
            _cobros.AgregarNotificacion(Nueva(ResultadoNotificacion.FIRMA_INVALIDA));
            await _unidadDeTrabajo.GuardarAsync(cancelacion);
            throw new ExcepcionDeAplicacion("firma_invalida", 401, "La firma de la notificación no es válida.");
        }

        try
        {
            return await _unidadDeTrabajo.EnTransaccionAsync(() => ProcesarAsync(verificada, cancelacion), cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out _))
        {
            // Otra copia de la misma notificación se guardó justo antes: no hay nada más que hacer.
            return ResultadoNotificacion.REPETIDA;
        }
    }

    private async Task<ResultadoNotificacion> ProcesarAsync(NotificacionVerificada recibida, CancellationToken cancelacion)
    {
        var intento = await _cobros.ObtenerIntentoPorReferenciaAsync(recibida.Referencia, cancelacion);
        if (intento is null)
        {
            return await RegistrarAsync(recibida, ResultadoNotificacion.REFERENCIA_DESCONOCIDA, null, cancelacion);
        }

        // El bloqueo del cobro pone en fila a las copias simultáneas de la misma notificación.
        var cobro = await _cobros.ObtenerBloqueandoAsync(intento.CobroId, cancelacion);
        if (cobro is null)
        {
            return await RegistrarAsync(recibida, ResultadoNotificacion.REFERENCIA_DESCONOCIDA, null, cancelacion);
        }

        if (await _cobros.ExisteNotificacionAsync(recibida.TransaccionId, recibida.Estado, cancelacion))
        {
            return await RegistrarAsync(recibida, ResultadoNotificacion.REPETIDA, cobro.Id, cancelacion, conTransaccion: false);
        }

        if (recibida.ValorEnPesos != cobro.Total)
        {
            return await RegistrarAsync(recibida, ResultadoNotificacion.VALOR_NO_COINCIDE, cobro.Id, cancelacion);
        }

        if (!string.Equals(recibida.Estado, EstadoAprobado, StringComparison.OrdinalIgnoreCase))
        {
            return await RegistrarAsync(recibida, ResultadoNotificacion.NO_APROBADA, cobro.Id, cancelacion);
        }

        if (cobro.Estado == EstadoCobroPlataforma.ANULADO)
        {
            return await RegistrarAsync(recibida, ResultadoNotificacion.PAGO_A_COBRO_ANULADO, cobro.Id, cancelacion);
        }

        if (cobro.Estado == EstadoCobroPlataforma.PAGADO || await _cobros.ObtenerPagoDeCobroAsync(cobro.Id, cancelacion) is not null)
        {
            return await RegistrarAsync(recibida, ResultadoNotificacion.PAGO_DUPLICADO, cobro.Id, cancelacion);
        }

        _cobros.AgregarPago(new PagoPlataforma
        {
            CobroId = cobro.Id,
            Valor = cobro.Total,
            PagadoEn = _reloj.AhoraUtc,
            Origen = OrigenPagoPlataforma.PASARELA,
            Referencia = recibida.Referencia,
            TransaccionId = recibida.TransaccionId,
            MedioDePago = recibida.MedioDePago,
        });
        cobro.Estado = EstadoCobroPlataforma.PAGADO;
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
        await _suspension.ReevaluarAsync(cobro.ClubId, cancelacion);

        return await RegistrarAsync(recibida, ResultadoNotificacion.APLICADA, cobro.Id, cancelacion);
    }

    private async Task<ResultadoNotificacion> RegistrarAsync(
        NotificacionVerificada recibida,
        ResultadoNotificacion resultado,
        Guid? cobroId,
        CancellationToken cancelacion,
        bool conTransaccion = true)
    {
        var notificacion = Nueva(resultado);
        // Una repetición no guarda la transacción: el índice único (transacción, estado) ya lo tiene la primera.
        notificacion.TransaccionId = conTransaccion ? recibida.TransaccionId : null;
        notificacion.EstadoPasarela = recibida.Estado;
        notificacion.Referencia = recibida.Referencia;
        notificacion.Valor = recibida.ValorEnPesos;
        notificacion.CobroId = cobroId;
        _cobros.AgregarNotificacion(notificacion);
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
        return resultado;
    }

    private NotificacionPasarela Nueva(ResultadoNotificacion resultado) =>
        new() { RecibidaEn = _reloj.AhoraUtc, Resultado = resultado };
}
