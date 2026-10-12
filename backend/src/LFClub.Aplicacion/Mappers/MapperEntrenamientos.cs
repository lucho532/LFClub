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
            miRespuesta);

    /// <summary>El detalle de un entrenamiento que quien pregunta puede ver.</summary>
    public static EntrenamientoDto ADetalle(EntrenamientoVisible visible)
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
            visible.MiAsistencia);
    }

    private static IReadOnlyList<CategoriaDeActividadDto> Categorias(Entrenamiento entrenamiento) =>
        MapperActividades.Categorias(entrenamiento.Categorias.Select(fila => (fila.CategoriaId, fila.Categoria)));
}
