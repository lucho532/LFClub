using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de un integrante y sus asignaciones en la ficha básica (spec 008).
/// Su responsabilidad es que ninguna entidad salga por la API (constitución §5), que de la persona
/// solo salga lo que enumera RF-001 y que las categorías salgan por año y los equipos por nombre.
/// No consulta la base de datos ni decide quién puede ver la ficha.
/// </summary>
public static class MapperFichaBasica
{
    /// <summary>
    /// La ficha básica de esa persona. Las asignaciones deben venir con su categoría y sus equipos
    /// cargados; una lista de equipos vacía significa que entrena la categoría en general.
    /// </summary>
    public static FichaBasicaDto AFichaBasica(
        PersonaConFoto persona, IReadOnlyList<AsignacionEntrenadorCategoria> asignaciones) => new(
        persona.Integrante.Id,
        persona.Integrante.Nombres,
        persona.Integrante.Apellidos,
        persona.Integrante.Rol,
        persona.VersionFoto,
        ACategorias(asignaciones));

    /// <summary>
    /// Las categorías que entrena, por año, con los nombres de sus equipos activos; una lista de equipos
    /// vacía significa que entrena la categoría en general. Las asignaciones deben venir con su categoría y
    /// sus equipos cargados.
    /// </summary>
    public static IReadOnlyList<CategoriaDeFichaBasicaDto> ACategorias(
        IEnumerable<AsignacionEntrenadorCategoria> asignaciones) => asignaciones
        .Select(asignacion => new CategoriaDeFichaBasicaDto(
            (asignacion.Categoria ?? throw new InvalidOperationException("La asignación debe venir con su categoría.")).Anio,
            asignacion.Equipos
                .Select(fila => fila.Equipo ?? throw new InvalidOperationException("Cada equipo debe venir cargado."))
                .Where(equipo => equipo.Activo)
                .Select(equipo => equipo.Nombre)
                .Order(OrdenDePersonas.Comparador)
                .ToList()))
        .OrderBy(categoria => categoria.Anio)
        .ToList();
}
