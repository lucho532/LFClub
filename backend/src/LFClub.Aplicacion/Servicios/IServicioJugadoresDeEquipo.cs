using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso con los que el PRESIDENTE decide en qué equipos juega cada jugador
/// de una categoría (constitución §11.3; RF-025 y RF-026).
/// Su responsabilidad es poner a un jugador en un equipo de su categoría y sacarlo de él, sin
/// tocar los demás equipos en los que esté. Las dos responden con la categoría ya actualizada.
/// No pone a nadie en un equipo de una categoría que no es la suya y no comprueba el rol de quien
/// llama: lo hace la autorización, que tampoco deja hacerlo a los entrenadores de la categoría.
/// </summary>
public interface IServicioJugadoresDeEquipo
{
    /// <summary>Pone al jugador en el equipo; si ya estaba, no cambia nada.</summary>
    Task<CategoriaDetalleDto> PonerAsync(
        Guid categoriaId, Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Saca al jugador del equipo; si no estaba, no cambia nada.</summary>
    Task<CategoriaDetalleDto> SacarAsync(
        Guid categoriaId, Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion = default);
}
