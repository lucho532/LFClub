namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el rol de una persona en la plataforma (constitución §8).
/// Su responsabilidad es nombrar los cinco roles con cuenta.
/// No decide qué puede hacer cada rol: eso lo resuelven la autorización y las reglas del dominio.
/// </summary>
public enum Rol
{
    /// <summary>Dueño de la plataforma. Es una única cuenta y no pertenece a ningún club.</summary>
    DESARROLLADOR,

    /// <summary>Presidente y dueño de un club.</summary>
    PRESIDENTE,

    /// <summary>Miembro de la directiva de un club.</summary>
    DIRECTIVO,

    /// <summary>Profesor a cargo de una o varias categorías de un club.</summary>
    ENTRENADOR,

    /// <summary>Jugador inscrito en un club; la cuenta la usa su familia.</summary>
    JUGADOR,
}
