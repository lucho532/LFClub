using System.Text.Json.Serialization;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa el detalle de un entrenamiento tal como lo recibe quien lo ve (spec 009, RF-003).
/// Su responsabilidad es llevar lo del resumen, su nota, lo que quien pregunta puede hacer con él
/// y, solo para la cuenta de un jugador, su propia respuesta y su propia asistencia.
/// No lleva respuestas ni asistencia de otros jugadores (RF-024): <see cref="Nota"/>,
/// <see cref="MiRespuesta"/> y <see cref="MiAsistencia"/> se omiten del JSON cuando son nulos.
/// </summary>
/// <param name="EntrenamientoId">Identificador del entrenamiento.</param>
/// <param name="Fecha">Fecha.</param>
/// <param name="HoraInicio">Hora de inicio, <c>HH:mm</c>.</param>
/// <param name="HoraFin">Hora de fin, <c>HH:mm</c>.</param>
/// <param name="Lugar">Lugar.</param>
/// <param name="Estado">Programado o cancelado.</param>
/// <param name="Categorias">Sus categorías, por año.</param>
/// <param name="MiRespuesta">La respuesta de la familia del jugador de la petición; ausente si no respondió.</param>
/// <param name="Nota">Nota para las familias; ausente si no tiene.</param>
/// <param name="Permisos">Lo que quien pregunta puede hacer con él.</param>
/// <param name="MiAsistencia">Si el jugador de la petición asistió; ausente si nadie la registró.</param>
public record EntrenamientoDto(
    Guid EntrenamientoId,
    DateOnly Fecha,
    string HoraInicio,
    string HoraFin,
    string Lugar,
    EstadoEntrenamiento Estado,
    IReadOnlyList<CategoriaDeActividadDto> Categorias,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RespuestaFamilia? MiRespuesta,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Nota,
    PermisosDeActividadDto Permisos,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? MiAsistencia);
