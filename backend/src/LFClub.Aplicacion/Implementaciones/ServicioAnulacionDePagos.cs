using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que anula un pago confirmado.
/// Su responsabilidad es validar el motivo, bloquear al jugador del pago para que una anulación y una
/// confirmación sobre él se ejecuten una detrás de otra, comprobar con <see cref="ReglaTransicionPago"/>
/// que el pago sigue confirmado y guardar quién lo anuló, cuándo y por qué, con su nombre copiado (§13).
/// Las aplicaciones del pago se conservan: el cálculo del saldo ignora las de un pago anulado, así que
/// los cargos vuelven a deberse. El recibo y el soporte se conservan.
/// No edita ni borra el pago, no comprueba el rol de quien anula y no accede al contexto de Entity
/// Framework ni conoce HTTP.
/// </summary>
public class ServicioAnulacionDePagos : IServicioAnulacionDePagos
{
    private readonly IRepositorioPagos _pagos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAnulacionDePagos(IRepositorioPagos pagos, IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj)
    {
        _pagos = pagos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PagoDto> AnularAsync(
        Guid pagoId, MotivoDto datos, UsuarioRol quienAnula, CancellationToken cancelacion = default)
    {
        var motivo = ValidadorPago.ValidarMotivo(datos);
        var ahora = _reloj.AhoraUtc;

        return await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var previo = await _pagos.ObtenerSinSeguirAsync(pagoId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (previo.UsuarioRolId is { } jugadorId)
                {
                    await _pagos.BloquearJugadorAsync(quienAnula.ClubId, jugadorId, cancelacion);
                }

                var pago = await _pagos.ObtenerAsync(pagoId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (!ReglaTransicionPago.PuedeAnular(pago.Estado))
                {
                    throw ErroresDePagos.PagoNoEstaConfirmado();
                }

                pago.Estado = EstadoPago.ANULADO;
                pago.AnuladoEn = ahora;
                pago.AnuladoPorUsuarioId = quienAnula.UsuarioId;
                pago.AnuladoPorNombre = $"{quienAnula.Nombres} {quienAnula.Apellidos}";
                pago.MotivoAnulacion = motivo;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);

                var conSoporte = await _pagos.IdsConSoporteAsync([pago.Id], cancelacion);
                return MapperPagos.APago(pago, conSoporte.Contains(pago.Id));
            },
            cancelacion);
    }
}
