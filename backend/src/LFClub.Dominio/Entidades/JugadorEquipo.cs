namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa que un jugador juega en un equipo de su categoría (RF-025).
/// Su responsabilidad es guardar la pareja jugador y equipo; un jugador puede tener varias o
/// ninguna (RF-026).
/// No guarda historial: la fila se borra cuando el jugador sale del equipo, cambia de categoría o
/// se retira (RF-027, RF-042).
/// </summary>
public class JugadorEquipo : IPerteneceAClub
{
    /// <summary>Jugador. Parte de la clave primaria.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <summary>Equipo. Parte de la clave primaria.</summary>
    public Guid EquipoId { get; set; }

    /// <summary>Equipo.</summary>
    public Equipo? Equipo { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }
}
