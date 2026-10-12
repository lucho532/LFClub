namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa a qué ficha lleva el nombre de una persona del club (spec 008, RF-010).
/// Su responsabilidad es distinguir la ficha del jugador de la ficha básica de un PRESIDENTE,
/// DIRECTIVO o ENTRENADOR. Se serializa como texto, igual que <see cref="Rol"/>.
/// No decide si quien mira puede abrir esa ficha: eso lo resuelven
/// <c>ReglaEnlaceDeFicha</c> y, al abrirla, las reglas de acceso de siempre.
/// </summary>
public enum TipoFicha
{
    /// <summary>Ficha del jugador de la 005 y la 007.</summary>
    JUGADOR,

    /// <summary>Ficha básica de un PRESIDENTE, DIRECTIVO o ENTRENADOR.</summary>
    BASICA,
}
