using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso con los que el PRESIDENTE gestiona las categorías de su club
/// (constitución §11 y §14; RF-001 a RF-007 y RF-010).
/// Su responsabilidad es crear una categoría por año de nacimiento, desactivarla, reactivarla y
/// borrar la que nunca se usó, recogiendo en ella a los jugadores sin categoría de ese año al
/// crearla y al reactivarla.
/// No cambia el año de una categoría, no comprueba el rol de quien llama (lo hace la autorización)
/// y no gestiona sus equipos ni sus entrenadores.
/// </summary>
public interface IServicioCategorias
{
    /// <summary>Crea la categoría de ese año y recoge a los jugadores sin categoría nacidos en él.</summary>
    Task<CategoriaConUbicadosDto> CrearAsync(CrearCategoriaDto datos, CancellationToken cancelacion = default);

    /// <summary>
    /// Desactiva una categoría sin jugadores; sus entrenadores dejan de estar asignados. Si ya
    /// estaba inactiva no hace nada.
    /// </summary>
    Task<CategoriaDto> DesactivarAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>
    /// Reactiva una categoría, sin entrenadores, y recoge a los jugadores sin categoría nacidos en
    /// su año. Si ya estaba activa no hace nada.
    /// </summary>
    Task<CategoriaConUbicadosDto> ReactivarAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>Borra, con sus equipos, una categoría que nunca tuvo jugadores ni entrenadores.</summary>
    Task BorrarAsync(Guid categoriaId, CancellationToken cancelacion = default);
}
