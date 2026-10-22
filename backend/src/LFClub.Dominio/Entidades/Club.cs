using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa una escuela o equipo que usa la plataforma.
/// Su responsabilidad es guardar la configuración, la identidad y el estado actual del club. La
/// identidad (colores y escudo) la cambian el DESARROLLADOR desde el panel y el PRESIDENTE desde
/// "Datos del club" (constitución §7.2); vale el último cambio.
/// No guarda el escudo (va aparte para no cargarlo en cada consulta) ni un historial de estados
/// (§18): solo quién hizo el último cambio y cuándo.
/// </summary>
public class Club
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Nombre visible del club. Obligatorio.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Nombre en minúsculas y sin espacios sobrantes. Único en la plataforma (RF-007).</summary>
    public string NombreNormalizado { get; set; } = string.Empty;

    /// <summary>Sede. Opcional.</summary>
    public string? Sede { get; set; }

    /// <summary>Dirección. Opcional.</summary>
    public string? Direccion { get; set; }

    /// <summary>Correo de contacto. Opcional.</summary>
    public string? CorreoContacto { get; set; }

    /// <summary>Teléfono de contacto. Opcional.</summary>
    public string? TelefonoContacto { get; set; }

    /// <summary>Color principal en formato <c>#RRGGBB</c>. Nulo significa identidad neutra.</summary>
    public string? ColorPrincipal { get; set; }

    /// <summary>Color de acento en formato <c>#RRGGBB</c>. Opcional.</summary>
    public string? ColorAcento { get; set; }

    /// <summary>Sube cada vez que cambia el escudo; 0 significa que no tiene.</summary>
    public int VersionEscudo { get; set; }

    /// <summary>Estado actual. Un club nace activo.</summary>
    public EstadoClub Estado { get; set; } = EstadoClub.ACTIVO;

    /// <summary>Quién hizo el último cambio de estado (RF-031). Nulo hasta el primero.</summary>
    public Guid? EstadoCambiadoPorUsuarioId { get; set; }

    /// <summary>Cuándo se hizo el último cambio de estado (RF-031). Nulo hasta el primero.</summary>
    public DateTime? EstadoCambiadoEn { get; set; }

    /// <summary>
    /// Valor de la mensualidad del club, en pesos enteros (spec 015, RF-011). Nulo significa que el
    /// PRESIDENTE todavía no lo definió y entonces no se generan cargos.
    /// </summary>
    public decimal? ValorMensualidad { get; set; }

    /// <summary>
    /// Desde qué mes (primer día) se cobra al club el uso de la plataforma (spec 017). Nulo significa
    /// que no se le cobra. Lo fija el DESARROLLADOR.
    /// </summary>
    public DateOnly? MesInicioCobro { get; set; }

    /// <summary>Por qué está suspendido (spec 017). Solo tiene valor si <see cref="Estado"/> es <c>SUSPENDIDO</c>.</summary>
    public MotivoSuspension? MotivoSuspension { get; set; }

    /// <summary>Último número de recibo dado en el club; el siguiente es este más uno (research §7).</summary>
    public int UltimoRecibo { get; set; }

    /// <summary>Fecha de creación, en UTC.</summary>
    public DateTime CreadoEn { get; set; }
}
