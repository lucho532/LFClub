namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado actual de un club (constitución §7.4).
/// Su responsabilidad es nombrar los estados posibles.
/// No define qué transiciones se permiten ni quién entra en cada estado: eso está en las reglas.
/// </summary>
public enum EstadoClub
{
    /// <summary>El club funciona con normalidad.</summary>
    ACTIVO,

    /// <summary>Solo entra su presidente; el resto ve un aviso de incidencia temporal.</summary>
    SUSPENDIDO,

    /// <summary>No entra nadie del club. Es el único estado desde el que se puede eliminar.</summary>
    DADO_DE_BAJA,
}
