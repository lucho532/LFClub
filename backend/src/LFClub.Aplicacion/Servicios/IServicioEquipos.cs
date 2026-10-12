using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso con los que el PRESIDENTE divide una categoría en equipos
/// (constitución §11.3 y §14; RF-022 a RF-024a y RF-030).
/// Su responsabilidad es crear un equipo con nombre dentro de una categoría activa, renombrarlo,
/// desactivarlo y borrar el que nunca se usó. Todas responden con la categoría ya actualizada.
/// No pasa un equipo a otra categoría ni lo reactiva, y no comprueba el rol de quien llama: lo
/// hace la autorización.
/// </summary>
public interface IServicioEquipos
{
    /// <summary>Crea un equipo vacío en la categoría.</summary>
    Task<CategoriaDetalleDto> CrearAsync(Guid categoriaId, NombreEquipoDto datos, CancellationToken cancelacion = default);

    /// <summary>Cambia el nombre de un equipo; conserva sus jugadores y sus entrenadores.</summary>
    Task<CategoriaDetalleDto> RenombrarAsync(
        Guid categoriaId, Guid equipoId, NombreEquipoDto datos, CancellationToken cancelacion = default);

    /// <summary>
    /// Desactiva un equipo: deja de mostrarse, sus jugadores siguen en la categoría y sus
    /// entrenadores siguen asignados a ella.
    /// </summary>
    Task<CategoriaDetalleDto> DesactivarAsync(Guid categoriaId, Guid equipoId, CancellationToken cancelacion = default);

    /// <summary>Borra un equipo que nunca tuvo jugadores ni entrenadores.</summary>
    Task<CategoriaDetalleDto> BorrarAsync(Guid categoriaId, Guid equipoId, CancellationToken cancelacion = default);
}
