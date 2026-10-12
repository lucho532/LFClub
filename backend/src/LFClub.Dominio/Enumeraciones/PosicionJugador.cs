namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa la posición en la que juega un jugador, tal como sale en su carta (spec 010, RF-027).
/// Su responsabilidad es nombrar las cuatro posiciones que se pueden elegir; no tener posición es la
/// ausencia de valor.
/// No decide quién la cambia ni cómo se abrevia en la carta.
/// </summary>
public enum PosicionJugador
{
    /// <summary>Portero (POR en la carta).</summary>
    PORTERO,

    /// <summary>Defensa (DEF en la carta).</summary>
    DEFENSA,

    /// <summary>Mediocampista (MED en la carta).</summary>
    MEDIOCAMPISTA,

    /// <summary>Delantero (DEL en la carta).</summary>
    DELANTERO,
}
