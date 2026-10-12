using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa un comunicado que el equipo del club publica para las familias (constitución §14.4;
/// spec 011, RF-001 a RF-011).
/// Su responsabilidad es guardar el título y el texto plano tal como se publicaron, la audiencia
/// (<see cref="Alcance"/>), la clave que hace idempotente la publicación, quién lo publicó (su
/// integrante, y su nombre y rol copiados en ese momento, §13) y, si se retiró, cuándo y quién. De
/// él cuelgan sus destinos y sus destinatarios.
/// No se edita ni se borra: el retiro es el único cambio posible, y no tiene historial (§18). No
/// decide quién lo recibe ni quién lo ve: eso es de las reglas del dominio.
/// </summary>
public class Comunicado : IPerteneceAClub
{
    /// <summary>Longitud máxima del título.</summary>
    public const int LongitudMaximaDelTitulo = 100;

    /// <summary>Longitud máxima del texto.</summary>
    public const int LongitudMaximaDelTexto = 1000;

    /// <summary>Longitud máxima de un nombre de persona copiado.</summary>
    public const int LongitudMaximaDelNombre = 161;

    /// <summary>Clave primaria; v7, así que ordena por creación y sirve de cursor de paginación.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }

    /// <summary>Título. Obligatorio, de 100 caracteres como máximo, recortado.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Texto plano. Obligatorio, de 1.000 caracteres como máximo, recortado.</summary>
    public string Texto { get; set; } = string.Empty;

    /// <summary>A quién va dirigido.</summary>
    public AlcanceComunicado Alcance { get; set; }

    /// <summary>Clave de publicación que genera la pantalla; única por club (research §6).</summary>
    public Guid Clave { get; set; }

    /// <summary>Fecha y hora de publicación, en UTC.</summary>
    public DateTime PublicadoEn { get; set; }

    /// <summary>
    /// Integrante que lo publicó. Foránea a <see cref="UsuarioRol"/>; pasa a nulo si ese integrante
    /// se borra.
    /// </summary>
    public Guid? PublicadoPorUsuarioRolId { get; set; }

    /// <summary>Nombres y apellidos de quien lo publicó, copiados en ese momento (§13).</summary>
    public string PublicadoPorNombre { get; set; } = string.Empty;

    /// <summary>Rol que tenía al publicarlo: PRESIDENTE, DIRECTIVO o ENTRENADOR; copiado (§13).</summary>
    public Rol PublicadoPorRol { get; set; }

    /// <summary>Fecha y hora del retiro, en UTC. Nulo mientras está publicado.</summary>
    public DateTime? RetiradoEn { get; set; }

    /// <summary>Integrante que lo retiró. Foránea a <see cref="UsuarioRol"/>; a nulo si se borra.</summary>
    public Guid? RetiradoPorUsuarioRolId { get; set; }

    /// <summary>Nombres y apellidos de quien lo retiró, copiados en ese momento (§13).</summary>
    public string? RetiradoPorNombre { get; set; }

    /// <summary>Categorías o equipos a los que va dirigido; vacío con alcance <c>CLUB</c>.</summary>
    public List<ComunicadoDestino> Destinos { get; set; } = [];

    /// <summary>Jugadores activos que lo recibieron al publicarlo, fijos desde entonces.</summary>
    public List<ComunicadoDestinatario> Destinatarios { get; set; } = [];
}
