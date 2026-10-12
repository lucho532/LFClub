namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa la rareza de la carta de un jugador, que premia su asistencia a los entrenamientos de
/// la temporada (spec 010, RF-026).
/// Su responsabilidad es nombrar los tres niveles, de menos a más.
/// No se guarda: se calcula al consultar la carta con <c>ReglaRarezaDeCarta</c>.
/// </summary>
public enum RarezaCarta
{
    /// <summary>Menos del 60 % de asistencia, o sin asistencias registradas.</summary>
    BRONCE,

    /// <summary>Del 60 % al 84 %.</summary>
    PLATA,

    /// <summary>85 % o más.</summary>
    ORO,
}
