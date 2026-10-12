namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa la asignación de un integrante como entrenador de una categoría de su club
/// (constitución §8 y §12.3).
/// Su responsabilidad es guardar la pareja integrante y categoría, si la asignación está activa y
/// qué equipos de esa categoría dirige. Es lo que determina qué categorías ve un ENTRENADOR y, en
/// lo deportivo, quién entrena una categoría: sea ENTRENADOR, DIRECTIVO o PRESIDENTE, la asignación
/// activa le deja programar entrenamientos, convocar, registrar resultados y asistencia, y ver los
/// partidos próximos de esa categoría (constitución 4.6.0, §8 y §17.2; spec 009).
/// No cambia el rol del integrante: un PRESIDENTE o un DIRECTIVO asignado conserva su único rol y
/// su alcance fuera de lo deportivo (RF-017a). No se borra al retirarla: solo deja de estar activa
/// (RF-021).
/// </summary>
public class AsignacionEntrenadorCategoria : IPerteneceAClub
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }

    /// <summary>Categoría asignada. Obligatorio.</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoría asignada.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>Integrante asignado. Obligatorio.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Integrante asignado.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <summary>Obligatorio. Falso desde que se retira o se desactiva la categoría.</summary>
    public bool Activa { get; set; } = true;

    /// <summary>Fecha de creación, en UTC.</summary>
    public DateTime CreadaEn { get; set; }

    /// <summary>Equipos de la categoría que dirige; puede no haber ninguno.</summary>
    public List<EntrenadorEquipo> Equipos { get; set; } = [];
}
