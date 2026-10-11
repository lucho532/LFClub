using System.Text.Json.Serialization;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa un partido en la lista de partidos y en el calendario (spec 009).
/// Su responsabilidad es llevar el rival, la fecha y la hora en hora de Colombia, el lugar, la
/// condición, el tipo y el torneo, el estado, si le falta el resultado, el marcador si está jugado,
/// sus categorías y, solo para la cuenta de un jugador convocado que respondió, su propia respuesta.
/// No lleva convocados ni quiénes jugaron (RF-012): lo que no aplica se omite del JSON.
/// </summary>
/// <param name="PartidoId">Identificador del partido.</param>
/// <param name="Rival">Equipo rival.</param>
/// <param name="Fecha">Fecha.</param>
/// <param name="Hora">Hora, <c>HH:mm</c>.</param>
/// <param name="Lugar">Lugar.</param>
/// <param name="Condicion">Local o visitante.</param>
/// <param name="Tipo">Amistoso o de torneo.</param>
/// <param name="NombreTorneo">Nombre del torneo; ausente en un amistoso.</param>
/// <param name="Estado">Próximo, cancelado o jugado.</param>
/// <param name="FaltaResultado">Verdadero si está próximo y ya empezó.</param>
/// <param name="GolesClub">Goles del club; ausente si no está jugado.</param>
/// <param name="GolesRival">Goles del rival; ausente si no está jugado.</param>
/// <param name="Categorias">Sus categorías, por año.</param>
/// <param name="MiRespuesta">La respuesta de la familia del convocado de la petición; ausente si no respondió.</param>
public record PartidoResumenDto(
    Guid PartidoId,
    string Rival,
    DateOnly Fecha,
    string Hora,
    string Lugar,
    CondicionPartido Condicion,
    TipoPartido Tipo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NombreTorneo,
    EstadoPartido Estado,
    bool FaltaResultado,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? GolesClub,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? GolesRival,
    IReadOnlyList<CategoriaDeActividadDto> Categorias,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RespuestaFamilia? MiRespuesta);
