using System.Text.Json.Serialization;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa el detalle de un partido tal como lo recibe quien lo ve (spec 009).
/// Su responsabilidad es llevar lo del resumen, lo que quien pregunta puede hacer con él y, si está
/// jugado, sus goleadores para todos y, para quien ve la planilla, el último cambio del resultado,
/// quién lo publicó primero y el origen del último cambio. A la cuenta de un convocado le añade lo
/// de su propio jugador (spec 010).
/// No lleva convocados ni quiénes jugaron, ni lo indicado por otras familias (RF-012 de la 009,
/// RF-005 de la 010): eso es de la planilla. Lo que no aplica se
/// omite del JSON.
/// </summary>
/// <param name="PartidoId">Identificador del partido.</param>
/// <param name="Rival">Equipo rival; ausente si está por definir.</param>
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
/// <param name="Permisos">Lo que quien pregunta puede hacer con él.</param>
/// <param name="Goleadores">Sus goleadores, de mayor a menor; solo en un partido jugado.</param>
/// <param name="UltimoCambioResultado">Quién y cuándo cambió el resultado; solo jugado y para quien ve la planilla.</param>
/// <param name="MiParticipacion">Lo del jugador de la petición; solo para la cuenta de un convocado a un partido que ya empezó y no está cancelado (spec 010).</param>
/// <param name="Publicacion">Quién puso el marcador primero; solo jugado y para quien ve la planilla (spec 010).</param>
/// <param name="OrigenResultado">Quién lo cambió por última vez; solo jugado y para quien ve la planilla (spec 010).</param>
public record PartidoDto(
    Guid PartidoId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Rival,
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
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RespuestaFamilia? MiRespuesta,
    PermisosDeActividadDto Permisos,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<GoleadorDePartidoDto>? Goleadores,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] UltimoCambioDto? UltimoCambioResultado,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MiParticipacionDto? MiParticipacion = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PublicacionResultadoDto? Publicacion = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] OrigenResultado? OrigenResultado = null);
