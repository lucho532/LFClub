namespace LaPecosa.Dominio.Enumeraciones;

/// <summary>
/// Representa si el club juega un partido en casa o fuera (spec 009, RF-007).
/// Su responsabilidad es nombrar las dos condiciones, que deciden cómo se escribe el marcador.
/// No guarda el lugar del partido, que es un texto aparte.
/// </summary>
public enum CondicionPartido
{
    /// <summary>El club juega en casa.</summary>
    LOCAL,

    /// <summary>El club juega fuera.</summary>
    VISITANTE,
}
