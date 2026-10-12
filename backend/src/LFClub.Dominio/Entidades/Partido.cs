using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa un partido del club para una o varias categorías (spec 009, RF-007 a RF-021).
/// Su responsabilidad es guardar el rival, la fecha y la hora en hora de Colombia, el lugar, la
/// condición, el tipo y el torneo, su estado actual, el marcador y quién lo cambió por última vez,
/// con su nombre copiado en ese momento (§13). El resultado lo publica la familia de un convocado o
/// lo registra quien gestiona (§17.3, spec 010): guarda el origen del último cambio y la primera
/// publicación (cuándo, quién y si fue una familia), que no cambia aunque después se corrija. Las
/// columnas <c>ResultadoCambiado…</c> siguen siendo el último cambio. De él cuelgan sus categorías y
/// sus convocados.
/// No decide quién lo ve: mientras no se juega solo existe para sus convocados, quienes entrenan
/// alguna de sus categorías y el PRESIDENTE (§17.2), y eso lo deciden las reglas del dominio. No
/// guarda historial del resultado (§18) y nunca se borra. Puede ser la actividad de un aviso
/// (<see cref="AvisoDeActividad"/>, spec 012), que lo referencia por tipo e identificador sin
/// foránea.
/// </summary>
public class Partido : IPerteneceAClub
{
    /// <summary>Longitud máxima del rival, el lugar y el nombre del torneo.</summary>
    public const int LongitudMaximaDeTexto = 120;

    /// <summary>Máximo de goles de cada lado en un marcador.</summary>
    public const int MaximoDeGoles = 99;

    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }

    /// <summary>
    /// Equipo rival, de 120 caracteres como máximo. Nulo mientras no se sabe: las pantallas lo
    /// muestran como "Por definir" y se escribe después al cambiar el partido.
    /// </summary>
    public string? Rival { get; set; }

    /// <summary>Fecha del partido, en hora de Colombia. Obligatoria.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora de inicio, en hora de Colombia. Obligatoria.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Lugar. Obligatorio, de 120 caracteres como máximo.</summary>
    public string Lugar { get; set; } = string.Empty;

    /// <summary>Local o visitante.</summary>
    public CondicionPartido Condicion { get; set; }

    /// <summary>Amistoso o de torneo.</summary>
    public TipoPartido Tipo { get; set; }

    /// <summary>
    /// Nombre del torneo, de 120 caracteres como máximo. Obligatorio si <see cref="Tipo"/> es
    /// <see cref="TipoPartido.TORNEO"/>; nulo en un amistoso.
    /// </summary>
    public string? NombreTorneo { get; set; }

    /// <summary>Estado actual. Nace <see cref="EstadoPartido.PROXIMO"/>.</summary>
    public EstadoPartido Estado { get; set; } = EstadoPartido.PROXIMO;

    /// <summary>
    /// Goles del club, de 0 a 99. Nulo hasta registrar el resultado; con valor si y solo si está
    /// jugado.
    /// </summary>
    public int? GolesClub { get; set; }

    /// <summary>
    /// Goles del rival, de 0 a 99. Nulo hasta registrar el resultado; con valor si y solo si está
    /// jugado.
    /// </summary>
    public int? GolesRival { get; set; }

    /// <summary>Última vez que se registró o corrigió el resultado, en UTC (RF-020).</summary>
    public DateTime? ResultadoCambiadoEn { get; set; }

    /// <summary>
    /// Cuenta de quien registró o corrigió el resultado por última vez. Foránea a
    /// <see cref="Usuario"/>; pasa a nulo si esa cuenta se elimina.
    /// </summary>
    public Guid? ResultadoCambiadoPorUsuarioId { get; set; }

    /// <summary>Nombres y apellidos de quien cambió el resultado, copiados en ese momento (§13).</summary>
    public string? ResultadoCambiadoPorNombre { get; set; }

    /// <summary>
    /// Quién puso el marcador por última vez. Nulo hasta el resultado. <see cref="OrigenResultado.FAMILIA"/>
    /// cuando lo publica una familia; <see cref="OrigenResultado.GESTION"/> en cuanto quien entrena o
    /// el PRESIDENTE lo registra o corrige, y ya no vuelve a FAMILIA.
    /// </summary>
    public OrigenResultado? OrigenResultado { get; set; }

    /// <summary>Primera vez que se puso el marcador, en UTC; no cambia después (RF-009).</summary>
    public DateTime? PublicadoEn { get; set; }

    /// <summary>Cuenta que lo puso primero. Foránea a <see cref="Usuario"/>; pasa a nulo si se elimina.</summary>
    public Guid? PublicadoPorUsuarioId { get; set; }

    /// <summary>
    /// Nombre de quien lo puso primero (texto ≤ 161), copiado (§13): el del jugador si fue su familia.
    /// </summary>
    public string? PublicadoPorNombre { get; set; }

    /// <summary>Verdadero si la primera publicación fue de una familia.</summary>
    public bool PublicadoPorFamilia { get; set; }

    /// <summary>Fecha y hora de creación, en UTC.</summary>
    public DateTime CreadoEn { get; set; }

    /// <summary>
    /// Integrante que lo creó. Foránea a <see cref="UsuarioRol"/>; pasa a nulo si ese integrante se
    /// borra.
    /// </summary>
    public Guid? CreadoPorUsuarioRolId { get; set; }

    /// <summary>Categorías del partido; una o más, y no cambian después de crearlo.</summary>
    public List<PartidoCategoria> Categorias { get; set; } = [];

    /// <summary>La convocatoria: un convocado por fila. Puede estar vacía.</summary>
    public List<PartidoJugador> Jugadores { get; set; } = [];
}
