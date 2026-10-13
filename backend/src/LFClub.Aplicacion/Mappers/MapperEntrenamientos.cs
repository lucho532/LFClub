using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de los entrenamientos en los DTOs del contrato (spec 009).
/// Su responsabilidad es armar el resumen y el detalle, con las horas como <c>HH:mm</c> y las
/// categorías por año, y poner la respuesta y la asistencia del jugador solo cuando se le pasan.
/// No decide quién ve qué ni consulta nada.
/// </summary>
public static class MapperEntrenamientos
{
    /// <summary>El entrenamiento en una lista, con la respuesta del jugador de la petición si se indica.</summary>
    public static EntrenamientoResumenDto AResumen(Entrenamiento entrenamiento, RespuestaFamilia? miRespuesta) =>
        new(
            entrenamiento.Id,
            entrenamiento.Fecha,
            MapperActividades.Hora(entrenamiento.HoraInicio),
            MapperActividades.Hora(entrenamiento.HoraFin),
            entrenamiento.Lugar,
            entrenamiento.Estado,
            Categorias(entrenamiento),
            miRespuesta,
            Equipos(entrenamiento));

    /// <summary>
    /// El detalle de un entrenamiento que quien pregunta puede ver. Los jugadores añadidos solo se
    /// entregan a quien dirige (spec 013, RF-018): para los demás se omiten del JSON.
    /// </summary>
    public static EntrenamientoDto ADetalle(EntrenamientoVisible visible, IReadOnlyList<JugadorSumableDto>? anadidos = null)
    {
        var entrenamiento = visible.Entrenamiento;
        return new EntrenamientoDto(
            entrenamiento.Id,
            entrenamiento.Fecha,
            MapperActividades.Hora(entrenamiento.HoraInicio),
            MapperActividades.Hora(entrenamiento.HoraFin),
            entrenamiento.Lugar,
            entrenamiento.Estado,
            Categorias(entrenamiento),
            visible.MiRespuesta,
            entrenamiento.Nota,
            visible.Permisos,
            visible.MiAsistencia,
            Equipos(entrenamiento),
            visible.Permisos.Dirige ? anadidos ?? [] : null);
    }

    private static IReadOnlyList<EquipoDeActividadDto> Equipos(Entrenamiento entrenamiento) => entrenamiento.Equipos
        .Where(fila => fila.Equipo is not null)
        .Select(fila => new EquipoDeActividadDto(
            fila.EquipoId, fila.Equipo!.Nombre, fila.Equipo.CategoriaId, fila.Equipo.Categoria?.Anio ?? 0, fila.Equipo.Activo))
        .OrderBy(equipo => equipo.Anio)
        .ThenBy(equipo => equipo.Nombre, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    private static IReadOnlyList<CategoriaDeActividadDto> Categorias(Entrenamiento entrenamiento) =>
        MapperActividades.Categorias(entrenamiento.Categorias.Select(fila => (fila.CategoriaId, fila.Categoria)));
}
