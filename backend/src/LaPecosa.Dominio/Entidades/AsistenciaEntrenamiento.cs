namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa la asistencia de un jugador a un entrenamiento, registrada por quien lo entrena o
/// por el PRESIDENTE (spec 009, RF-022).
/// Su responsabilidad es guardar si estuvo presente, la categoría con la que se registró por
/// primera vez y cuándo se escribió por última vez. Solo existe si alguien la registró: el
/// porcentaje de asistencia cuenta solo estas filas (RF-030).
/// No es la respuesta de la familia, que va aparte (RF-025), y su categoría no cambia aunque el
/// jugador cambie de categoría (§13, §11.2).
/// </summary>
public class AsistenciaEntrenamiento : IPerteneceAClub
{
    /// <summary>Entrenamiento. Parte de la clave primaria.</summary>
    public Guid EntrenamientoId { get; set; }

    /// <summary>Entrenamiento.</summary>
    public Entrenamiento? Entrenamiento { get; set; }

    /// <summary>Jugador. Parte de la clave primaria.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>
    /// Categoría del jugador al registrar su asistencia por primera vez. Obligatoria; no cambia al
    /// corregirla ni al cambiar el jugador de categoría.
    /// </summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoría con la que se registró.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>Verdadero si asistió; falso si faltó.</summary>
    public bool Presente { get; set; }

    /// <summary>Última vez que se escribió, en UTC.</summary>
    public DateTime RegistradaEn { get; set; }
}
