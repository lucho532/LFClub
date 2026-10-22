using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el inicio de un pago del cobro de la plataforma (spec 017, RF-008 y RF-009).
/// Su responsabilidad es crear un intento de pago con su referencia única y devolver la dirección de la
/// pasarela, ya firmada, para el valor del cobro guardado.
/// No cambia el estado del cobro: solo la notificación verificada de la pasarela lo marca pagado.
/// </summary>
public interface IServicioPagoPlataforma
{
    /// <summary>
    /// Crea el intento. <c>404</c> si el cobro no es del club; <c>409 cobro_no_pendiente</c> si ya está
    /// pagado o anulado; <c>503 pago_no_disponible</c> si la pasarela no está configurada.
    /// </summary>
    Task<IntentoDePagoDto> IniciarAsync(
        Guid clubId, Guid cobroId, Guid usuarioId, CancellationToken cancelacion = default);
}
