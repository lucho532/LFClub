using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa a un jugador convocado a un partido (constitución §13 y §17.2; spec 009).
/// Su responsabilidad es guardar la categoría con la que se le convocó, si jugó, sus goles, la
/// respuesta de su familia y cuándo se le convocó. Que exista la fila es lo que le deja ver un
/// partido que todavía no se juega. Guarda también quién indicó por última vez si jugó y sus goles
/// (spec 010, RF-009): su familia, desde la cuenta del jugador, o quien entrena o el PRESIDENTE,
/// que la corrigen cuando cambian lo que tenía o la marcan como que jugó sin indicación previa.
/// Una vez corregida, la familia ya no la cambia (RF-012).
/// No cambia su categoría aunque el jugador cambie de categoría (RF-018, RF-034). Al quitarlo de la
/// convocatoria se borra sin dejar rastro (RF-013), y al retirarlo se borran las de partidos que no
/// están jugados (RF-033).
/// </summary>
public class PartidoJugador : IPerteneceAClub
{
    /// <summary>Partido. Parte de la clave primaria.</summary>
    public Guid PartidoId { get; set; }

    /// <summary>Partido.</summary>
    public Partido? Partido { get; set; }

    /// <summary>Jugador convocado. Parte de la clave primaria.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador convocado.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Categoría del jugador al convocarlo. Obligatoria; no cambia (§13, RF-018).</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoría con la que se le convocó.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>Si jugó. Falso al convocar; lo fija el resultado.</summary>
    public bool Jugo { get; set; }

    /// <summary>Goles que marcó. Cero al convocar; solo mayor que cero si jugó (RF-017).</summary>
    public int Goles { get; set; }

    /// <summary>Respuesta de la familia; nula hasta que responde.</summary>
    public RespuestaFamilia? Respuesta { get; set; }

    /// <summary>Última vez que la familia respondió, en UTC.</summary>
    public DateTime? RespondidaEn { get; set; }

    /// <summary>
    /// Quién indicó lo de este convocado. Nulo: nadie indicó nada (RF-011). FAMILIA si lo indicó su
    /// familia; GESTION si lo corrigió quien entrena o el PRESIDENTE.
    /// </summary>
    public OrigenResultado? IndicadoPor { get; set; }

    /// <summary>Última vez que se indicó, en UTC.</summary>
    public DateTime? IndicadoEn { get; set; }

    /// <summary>Cuenta que lo indicó. Foránea a <see cref="Usuario"/>; pasa a nulo si se elimina.</summary>
    public Guid? IndicadoPorUsuarioId { get; set; }

    /// <summary>Nombre de quien lo indicó (texto ≤ 161), copiado (§13): el del jugador si fue su familia.</summary>
    public string? IndicadoPorNombre { get; set; }

    /// <summary>Fecha y hora de la convocatoria, en UTC.</summary>
    public DateTime ConvocadoEn { get; set; }
}
