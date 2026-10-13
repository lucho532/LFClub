using System.Text.Json.Serialization;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un entrenamiento en el calendario y en "Próximos entrenamientos" (spec 009).
/// Su responsabilidad es llevar su fecha, sus horas en hora de Colombia, su lugar, su estado y sus
/// categorías y, solo para la cuenta de un jugador que respondió, su propia respuesta.
/// No lleva respuestas ni asistencia de otros jugadores (RF-024): <see cref="MiRespuesta"/> se
/// omite del JSON cuando es nula.
/// </summary>
/// <param name="EntrenamientoId">Identificador del entrenamiento.</param>
/// <param name="Fecha">Fecha.</param>
/// <param name="HoraInicio">Hora de inicio, <c>HH:mm</c>.</param>
/// <param name="HoraFin">Hora de fin, <c>HH:mm</c>.</param>
/// <param name="Lugar">Lugar.</param>
/// <param name="Estado">Programado o cancelado.</param>
/// <param name="Categorias">Sus categorías, por año.</param>
/// <param name="MiRespuesta">La respuesta de la familia del jugador de la petición; ausente si no respondió.</param>
/// <param name="Equipos">Sus equipos, con su categoría y si siguen activos; vacío si es de toda la categoría (spec 013).</param>
public record EntrenamientoResumenDto(
    Guid EntrenamientoId,
    DateOnly Fecha,
    string HoraInicio,
    string HoraFin,
    string Lugar,
    EstadoEntrenamiento Estado,
    IReadOnlyList<CategoriaDeActividadDto> Categorias,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RespuestaFamilia? MiRespuesta,
    IReadOnlyList<EquipoDeActividadDto>? Equipos = null);
