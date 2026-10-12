using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso que cambian la carta de un jugador (spec 010, historia 3): el dorsal
/// y la posición, y las seis estadísticas.
/// Su responsabilidad es que solo cambie cada parte quien puede, sobre un jugador activo, con
/// valores válidos, y que quede quién lo cambió por última vez.
/// No conoce HTTP ni guarda historial (§18).
/// </summary>
public interface IServicioCarta
{
    /// <summary>Pone o cambia el dorsal y la posición; responde con la carta.</summary>
    Task<CartaDto> CambiarDorsalYPosicionAsync(
        Guid usuarioRolId, DorsalYPosicionDto datos, UsuarioRol quienCambia, CancellationToken cancelacion = default);

    /// <summary>Pone, cambia o quita las seis estadísticas; responde con la carta.</summary>
    Task<CartaDto> CambiarEstadisticasAsync(
        Guid usuarioRolId, EstadisticasCartaDto datos, UsuarioRol quienCambia, CancellationToken cancelacion = default);
}
