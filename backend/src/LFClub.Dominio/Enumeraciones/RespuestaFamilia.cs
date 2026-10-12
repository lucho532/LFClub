namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa lo que la familia de un jugador responde ante un entrenamiento o un partido al que
/// está convocado (spec 009, RF-023).
/// Su responsabilidad es nombrar las dos respuestas; no responder es la ausencia de respuesta.
/// No es la asistencia real, que registra quien entrena aparte (RF-025).
/// </summary>
public enum RespuestaFamilia
{
    /// <summary>El jugador asistirá.</summary>
    ASISTIRA,

    /// <summary>El jugador no asistirá.</summary>
    NO_ASISTIRA,
}
