using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de cambiar el estado de un club desde el panel (constitución §7.4,
/// RF-026 a RF-031).
/// Su responsabilidad es suspender, levantar la suspensión, dar de baja y revertir la baja,
/// dejando registrado quién hizo el último cambio y cuándo.
/// No modifica ningún otro dato del club ni lo elimina, y no guarda un historial de estados (§18).
/// </summary>
public interface IServicioEstadoClub
{
    /// <summary>Cambia el estado. Transición no permitida: 409 <c>transicion_no_permitida</c>.</summary>
    Task<ClubDetalleDto> CambiarAsync(
        Guid clubId, CambiarEstadoClubDto datos, Guid usuarioId, CancellationToken cancelacion = default);
}
