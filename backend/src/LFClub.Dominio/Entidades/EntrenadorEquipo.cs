namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa que una asignación de entrenador dirige un equipo de su categoría (RF-028).
/// Su responsabilidad es guardar la pareja asignación y equipo.
/// No cambia lo que ve el entrenador, que ve toda la categoría (RF-029), y no guarda historial: la
/// fila se borra cuando deja de dirigirlo.
/// </summary>
public class EntrenadorEquipo : IPerteneceAClub
{
    /// <summary>Asignación que dirige el equipo. Parte de la clave primaria.</summary>
    public Guid AsignacionEntrenadorCategoriaId { get; set; }

    /// <summary>Asignación que dirige el equipo.</summary>
    public AsignacionEntrenadorCategoria? Asignacion { get; set; }

    /// <summary>Equipo dirigido. Parte de la clave primaria.</summary>
    public Guid EquipoId { get; set; }

    /// <summary>Equipo dirigido.</summary>
    public Equipo? Equipo { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }
}
