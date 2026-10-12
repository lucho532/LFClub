namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa quién puso por última vez el resultado de un partido o lo de un convocado (spec 010,
/// research §1 y §2).
/// Su responsabilidad es distinguir lo que publicó o indicó la familia de un convocado de lo que
/// registró o corrigió quien gestiona: <see cref="GESTION"/> es quien entrena las categorías del
/// partido o el PRESIDENTE.
/// No dice quién en concreto lo hizo: eso lo guarda el nombre copiado junto a él (§13).
/// </summary>
public enum OrigenResultado
{
    /// <summary>La cuenta de un jugador convocado, es decir, su familia.</summary>
    FAMILIA,

    /// <summary>Quien entrena las categorías del partido o el PRESIDENTE.</summary>
    GESTION,
}
