namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa un enlace de un solo uso para crear una contraseña nueva.
/// Su responsabilidad es guardar el hash del token, su vencimiento y si ya se usó.
/// No guarda el token en claro ni la contraseña.
/// </summary>
public class SolicitudRecuperacion
{
    /// <summary>Minutos de vigencia del enlace (research §5, supuesto 3).</summary>
    public const int MinutosDeVigencia = 60;

    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Cuenta que pidió la recuperación.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Cuenta que pidió la recuperación.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>SHA-256 del token. Único.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Fecha de creación, en UTC.</summary>
    public DateTime CreadaEn { get; set; }

    /// <summary>Fecha de vencimiento: la de creación más 60 minutos.</summary>
    public DateTime VenceEn { get; set; }

    /// <summary>Fecha de uso. Nulo hasta que se usa o hasta que otra solicitud la deja sin efecto.</summary>
    public DateTime? UsadaEn { get; set; }

    /// <summary>Indica si el enlace todavía sirve en el momento indicado.</summary>
    public bool EstaVigente(DateTime ahoraUtc) => UsadaEn is null && ahoraUtc < VenceEn;
}
