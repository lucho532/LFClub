using System.Text.Json.Serialization;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la planilla de un partido: sus convocados con su respuesta, si jugaron y sus goles,
/// los conteos de respuestas, los equipos de sus categorías y, mientras está próximo, los jugadores
/// que se pueden convocar (spec 009, RF-010, RF-024; spec 013, RF-009).
/// Su responsabilidad es darle a quien entrena alguna de sus categorías y al PRESIDENTE lo que
/// necesita para convocar por equipo y registrar el resultado.
/// No llega nunca a la cuenta de un jugador (RF-012).
/// </summary>
/// <param name="PartidoId">Identificador del partido.</param>
/// <param name="Asistiran">Convocados cuya familia respondió que asistirá.</param>
/// <param name="NoAsistiran">Convocados cuya familia respondió que no asistirá.</param>
/// <param name="SinResponder">Convocados sin respuesta.</param>
/// <param name="Convocados">Los convocados, por la categoría copiada y apellidos.</param>
/// <param name="Convocables">Jugadores activos de sus categorías sin convocar; vacía si no está próximo.</param>
/// <param name="Equipos">Los equipos activos de las categorías del partido.</param>
public record PlanillaPartidoDto(
    Guid PartidoId,
    int Asistiran,
    int NoAsistiran,
    int SinResponder,
    IReadOnlyList<ConvocadoDto> Convocados,
    IReadOnlyList<ConvocableDto> Convocables,
    IReadOnlyList<EquipoDeActividadDto> Equipos);

/// <summary>
/// Representa a un convocado en la planilla de un partido.
/// Su responsabilidad es llevar su nombre, el enlace a su ficha si quien mira puede abrirla, la
/// categoría con la que se le convocó, la respuesta de su familia, si jugó, sus goles, quién lo
/// indicó, los equipos en los que juega hoy y si es un añadido de otra categoría.
/// No lleva datos personales del jugador.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="Enlace">Su ficha, si quien mira puede abrirla.</param>
/// <param name="Categoria">La copiada al convocarlo.</param>
/// <param name="Respuesta">Respuesta de su familia, o nula.</param>
/// <param name="Jugo">Si jugó.</param>
/// <param name="Goles">Goles que marcó.</param>
/// <param name="Indicacion">Quién indicó lo suyo y cuándo; ausente si nadie indicó nada (spec 010, RF-009).</param>
/// <param name="EquipoIds">Los equipos activos en los que juega hoy (spec 013).</param>
/// <param name="Anadido">Su categoría copiada no es una de las del partido; solo para quien gestiona (spec 013).</param>
public record ConvocadoDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    EnlaceFichaDto? Enlace,
    CategoriaDeActividadDto Categoria,
    RespuestaFamilia? Respuesta,
    bool Jugo,
    int Goles,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IndicacionDto? Indicacion = null,
    IReadOnlyList<Guid>? EquipoIds = null,
    bool Anadido = false);

/// <summary>
/// Representa a un jugador que se puede convocar a un partido próximo.
/// Su responsabilidad es llevar su nombre, su categoría actual y los equipos en los que juega hoy.
/// No lleva datos personales del jugador.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="Categoria">Su categoría actual.</param>
/// <param name="EquipoIds">Los equipos activos en los que juega hoy (spec 013).</param>
public record ConvocableDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    CategoriaDeActividadDto Categoria,
    IReadOnlyList<Guid>? EquipoIds = null);
