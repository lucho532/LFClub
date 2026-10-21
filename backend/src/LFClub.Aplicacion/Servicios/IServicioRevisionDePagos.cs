using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el circuito de las transferencias que informa la familia (spec 015, RF-021 y RF-022):
/// informar con un soporte, ver los pagos en revisión, confirmarlos y rechazarlos.
/// Su responsabilidad es dejar un pago informado en revisión sin tocar ningún saldo, y que solo el
/// PRESIDENTE lo confirme (con el valor y los cargos que él decida) o lo rechace (con un motivo que la
/// familia ve). Un pago rechazado es final: la familia informa uno nuevo.
/// No comprueba el rol (lo hace la autorización del controlador) y no mueve dinero (§6.2).
/// </summary>
public interface IServicioRevisionDePagos
{
    /// <summary>
    /// La familia informa una transferencia de su propio jugador con la imagen del comprobante. Queda
    /// <c>EN_REVISION</c>, sin aplicaciones ni recibo. Datos inválidos o soporte ausente, inválido o
    /// demasiado grande: 400; valor mayor que lo pendiente: 409; jugador ajeno: 404.
    /// </summary>
    Task<PagoDto> InformarAsync(
        Guid usuarioRolId,
        InformarPagoDto datos,
        byte[]? soporte,
        UsuarioRol quienInforma,
        CancellationToken cancelacion = default);

    /// <summary>Los pagos en revisión, del más antiguo al más reciente, con el jugador y sus cargos pendientes.</summary>
    Task<PagosEnRevisionDto> ListarEnRevisionAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Confirma un pago en revisión: puede corregir el valor y elegir los cargos; lo aplica, le da su
    /// recibo y guarda el saldo tras el pago. Ya no en revisión: 409 <c>pago_no_esta_en_revision</c>;
    /// valor mayor que lo pendiente: 409 <c>pago_excede_deuda</c>.
    /// </summary>
    Task<PagoDto> ConfirmarAsync(
        Guid pagoId, ConfirmarPagoDto? datos, UsuarioRol quienConfirma, CancellationToken cancelacion = default);

    /// <summary>Rechaza un pago en revisión con un motivo obligatorio; no cambia ningún saldo.</summary>
    Task<PagoDto> RechazarAsync(
        Guid pagoId, MotivoDto datos, UsuarioRol quienRechaza, CancellationToken cancelacion = default);
}
