namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el grupo sanguíneo de un jugador, con su factor Rh.
/// Su responsabilidad es nombrar los ocho valores admitidos en los datos clínicos de la ficha.
/// No indica si el dato se conoce: en la ficha es opcional y puede estar vacío.
/// </summary>
public enum GrupoSanguineo
{
    /// <summary>A positivo.</summary>
    A_POSITIVO,

    /// <summary>A negativo.</summary>
    A_NEGATIVO,

    /// <summary>B positivo.</summary>
    B_POSITIVO,

    /// <summary>B negativo.</summary>
    B_NEGATIVO,

    /// <summary>AB positivo.</summary>
    AB_POSITIVO,

    /// <summary>AB negativo.</summary>
    AB_NEGATIVO,

    /// <summary>O positivo.</summary>
    O_POSITIVO,

    /// <summary>O negativo.</summary>
    O_NEGATIVO,
}
