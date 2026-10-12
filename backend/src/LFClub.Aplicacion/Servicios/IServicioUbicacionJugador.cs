using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso con el que el PRESIDENTE ubica a un jugador sin categoría o lo pasa
/// de una categoría a otra (constitución §11.2; RF-014 a RF-016 y RF-027).
/// Su responsabilidad es dejar al jugador en la categoría elegida, sea o no la de su año de
/// nacimiento, y sacarlo de los equipos de la categoría anterior.
/// No crea otro jugador ni cambia ningún otro dato suyo (RF-015), no permite dejarlo sin categoría
/// y no comprueba el rol de quien llama: lo hace la autorización.
/// </summary>
public interface IServicioUbicacionJugador
{
    /// <summary>Ubica al jugador en la categoría y devuelve esa categoría con él ya dentro.</summary>
    Task<CategoriaDetalleDto> UbicarAsync(
        Guid usuarioRolId, UbicarJugadorDto datos, CancellationToken cancelacion = default);
}
