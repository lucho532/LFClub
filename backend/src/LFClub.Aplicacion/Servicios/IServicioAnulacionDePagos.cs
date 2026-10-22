using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de anular un pago confirmado (spec 015, RF-018 y RF-019; constitución §16.3).
/// Su responsabilidad es dejar el pago como anulado con quién lo anuló, cuándo y por qué, de modo que el
/// saldo del jugador se recalcule: el pago, su recibo y su resguardo siguen existiendo, marcados. Es la
/// única forma de deshacer un pago confirmado.
/// No edita ni borra el pago, no comprueba el rol (lo hace la autorización del controlador) y no mueve
/// dinero.
/// </summary>
public interface IServicioAnulacionDePagos
{
    /// <summary>
    /// Anula un pago confirmado con un motivo obligatorio. Sin motivo: 400; un pago que no está
    /// confirmado (en revisión, rechazado o ya anulado): 409 <c>pago_no_esta_confirmado</c>; de otro club
    /// o inexistente: 404.
    /// </summary>
    Task<PagoDto> AnularAsync(
        Guid pagoId, MotivoDto datos, UsuarioRol quienAnula, CancellationToken cancelacion = default);
}
