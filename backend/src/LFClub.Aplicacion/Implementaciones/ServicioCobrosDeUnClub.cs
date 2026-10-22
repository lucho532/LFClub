using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio con el que el DESARROLLADOR gestiona los cobros de un club (spec 017, RF-021 a
/// RF-023).
/// Su responsabilidad es listar el historial, marcar un cobro como pagado a mano (crea un pago con origen
/// <c>MANUAL</c>, quién y cuándo) y anularlo con motivo, cada cosa en una transacción que bloquea el cobro
/// y reevalúa la suspensión por impago del club.
/// No toca una suspensión manual y no devuelve datos personales.
/// </summary>
public class ServicioCobrosDeUnClub : IServicioCobrosDeUnClub
{
    private const int MinimoDelMotivo = 3;
    private const int MaximoDelMotivo = 300;

    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IServicioSuspensionPorImpago _suspension;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCobrosDeUnClub(
        IRepositorioClubesPlataforma clubes,
        IRepositorioCobrosPlataforma cobros,
        IServicioSuspensionPorImpago suspension,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _clubes = clubes;
        _cobros = cobros;
        _suspension = suspension;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CobroPlataformaDto>> ListarAsync(Guid clubId, CancellationToken cancelacion = default)
    {
        _ = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var cobros = await _cobros.ListarDeClubAsync(clubId, cancelacion);
        var pagos = await _cobros.ListarPagosAsync(cobros.Select(cobro => cobro.Id), cancelacion);

        return cobros.Select(cobro => MapperCobroPlataforma.ACobro(cobro, pagos.GetValueOrDefault(cobro.Id))).ToList();
    }

    /// <inheritdoc />
    public Task<CobroPlataformaDto> MarcarPagadoAsync(
        Guid clubId, Guid cobroId, Guid usuarioId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var cobro = await ObtenerPendienteAsync(clubId, cobroId, cancelacion);
                var pago = new PagoPlataforma
                {
                    CobroId = cobro.Id,
                    Valor = cobro.Total,
                    PagadoEn = _reloj.AhoraUtc,
                    Origen = OrigenPagoPlataforma.MANUAL,
                    RegistradoPorUsuarioId = usuarioId,
                };
                _cobros.AgregarPago(pago);
                cobro.Estado = EstadoCobroPlataforma.PAGADO;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
                await _suspension.ReevaluarAsync(clubId, cancelacion);

                return MapperCobroPlataforma.ACobro(cobro, pago);
            },
            cancelacion);

    /// <inheritdoc />
    public Task<CobroPlataformaDto> AnularAsync(
        Guid clubId, Guid cobroId, Guid usuarioId, AnularCobroDto datos, CancellationToken cancelacion = default)
    {
        var motivo = Validar(datos);

        return _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var cobro = await ObtenerPendienteAsync(clubId, cobroId, cancelacion);
                cobro.Estado = EstadoCobroPlataforma.ANULADO;
                cobro.AnuladoPorUsuarioId = usuarioId;
                cobro.AnuladoEn = _reloj.AhoraUtc;
                cobro.MotivoAnulacion = motivo;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
                await _suspension.ReevaluarAsync(clubId, cancelacion);

                return MapperCobroPlataforma.ACobro(cobro, null);
            },
            cancelacion);
    }

    private async Task<CobroPlataforma> ObtenerPendienteAsync(Guid clubId, Guid cobroId, CancellationToken cancelacion)
    {
        var cobro = await _cobros.ObtenerBloqueandoAsync(cobroId, cancelacion);
        if (cobro is null || cobro.ClubId != clubId)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        if (cobro.Estado != EstadoCobroPlataforma.PENDIENTE)
        {
            throw ExcepcionDeAplicacion.Conflicto("cobro_no_pendiente", "Este cobro ya está pagado o anulado.");
        }

        return cobro;
    }

    private static string Validar(AnularCobroDto datos)
    {
        var motivo = datos.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length is < MinimoDelMotivo or > MaximoDelMotivo)
        {
            var errores = new ErroresDeValidacion();
            errores.Agregar("motivo", $"El motivo es obligatorio y debe tener entre {MinimoDelMotivo} y {MaximoDelMotivo} caracteres.");
            errores.LanzarSiHayErrores();
        }

        return motivo;
    }
}
