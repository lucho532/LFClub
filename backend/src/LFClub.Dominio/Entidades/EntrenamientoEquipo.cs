namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa que un entrenamiento va dirigido a un equipo de una de sus categorías (spec 013,
/// RF-001; constitución §11.1 y §11.3).
/// Su responsabilidad es guardar la pareja entrenamiento y equipo. Un entrenamiento sin filas es de
/// toda la categoría (RF-025).
/// No mueve a ningún jugador de equipo y se conserva aunque el equipo se desactive después: el
/// entrenamiento sigue "con equipos".
/// </summary>
public class EntrenamientoEquipo : IPerteneceAClub
{
    /// <summary>Entrenamiento. Parte de la clave primaria.</summary>
    public Guid EntrenamientoId { get; set; }

    /// <summary>Entrenamiento.</summary>
    public Entrenamiento? Entrenamiento { get; set; }

    /// <summary>Equipo. Parte de la clave primaria.</summary>
    public Guid EquipoId { get; set; }

    /// <summary>Equipo.</summary>
    public Equipo? Equipo { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }
}
