using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la planilla de un entrenamiento: los jugadores de las categorías que quien pregunta
/// puede marcar, con la respuesta de su familia y su asistencia, y los conteos de respuestas (spec
/// 009, RF-022, RF-024; escenario 4.2).
/// Su responsabilidad es darle al PRESIDENTE y a quien entrena alguna de sus categorías lo que
/// necesita para registrar la asistencia.
/// No llega nunca a la cuenta de un jugador, y a quien entrena solo algunas categorías no le trae
/// los jugadores de las demás.
/// </summary>
/// <param name="EntrenamientoId">Identificador del entrenamiento.</param>
/// <param name="Asistiran">Jugadores de la planilla cuya familia respondió que asistirá.</param>
/// <param name="NoAsistiran">Jugadores de la planilla cuya familia respondió que no asistirá.</param>
/// <param name="SinResponder">Jugadores de la planilla sin respuesta.</param>
/// <param name="Jugadores">Por categoría y apellidos.</param>
public record PlanillaEntrenamientoDto(
    Guid EntrenamientoId,
    int Asistiran,
    int NoAsistiran,
    int SinResponder,
    IReadOnlyList<JugadorDePlanillaEntrenamientoDto> Jugadores);

/// <summary>
/// Representa a un jugador en la planilla de un entrenamiento.
/// Su responsabilidad es llevar su nombre, el enlace a su ficha si quien mira puede abrirla, su
/// categoría (la copiada si ya tiene asistencia), la respuesta de su familia, su asistencia y si
/// quien pregunta puede marcarla ahora.
/// No lleva datos personales del jugador.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="Enlace">Su ficha, si quien mira puede abrirla.</param>
/// <param name="Categoria">La copiada al registrar su asistencia o, si no tiene, la actual.</param>
/// <param name="Respuesta">Respuesta de su familia, o nula.</param>
/// <param name="Presente">Si asistió, o nulo si nadie la registró.</param>
/// <param name="Marcable">Si se puede registrar ahora: entrenamiento programado cuya fecha ya llegó.</param>
/// <param name="Anadido">Lo sumó una persona y no es de la audiencia natural (spec 013); solo se ve si quien pregunta dirige.</param>
public record JugadorDePlanillaEntrenamientoDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    EnlaceFichaDto? Enlace,
    CategoriaDeActividadDto Categoria,
    RespuestaFamilia? Respuesta,
    bool? Presente,
    bool Marcable,
    bool Anadido = false);
