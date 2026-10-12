using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de quitarle el rol a un presidente desde el panel (constitución §8,
/// RF-019a).
/// Su responsabilidad es aplicar la decisión del DESARROLLADOR (asignarle otro rol o eliminarlo
/// del club) siempre que el club conserve otro presidente registrado.
/// No permite dejar un club sin presidente ni otorga el rol PRESIDENTE.
/// </summary>
public interface IServicioRetiroPresidente
{
    /// <summary>
    /// Quita el rol al presidente. Si es el único del club: 409 <c>ultimo_presidente</c>. Al
    /// eliminarlo del club, si su cuenta se queda sin ningún club, la cuenta también se elimina.
    /// </summary>
    Task<ClubDetalleDto> RetirarAsync(
        Guid clubId, Guid usuarioRolId, RetirarPresidenteDto datos, CancellationToken cancelacion = default);
}
