using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa lo que la ficha añade a un jugador en un club (RF-001): el contacto de emergencia,
/// la seguridad social, los datos clínicos y quién la cambió por última vez.
/// Su responsabilidad es guardar esos datos, todos opcionales (RF-002), y el sello del último
/// cambio, que sustituye al anterior (RF-038). Hay como mucho una por jugador y club, y la fila
/// nace con el primer cambio: una ficha que nadie ha cambiado no tiene fila. Cargar o quitar la foto
/// del jugador también sella el último cambio, aunque la foto vive en otra tabla
/// (<see cref="FotoJugador"/>).
/// No guarda la identidad, la categoría ni los equipos, que viven en <see cref="UsuarioRol"/>, ni
/// el correo, el celular ni el responsable, que son de la cuenta (<see cref="Usuario"/>). No guarda
/// los archivos (<see cref="DocumentoJugador"/>), ni historial, ni valores anteriores (§18).
/// </summary>
public class FichaJugador : IPerteneceAClub
{
    /// <summary>Clave primaria y foránea a <see cref="UsuarioRol"/>: una ficha por jugador y club.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador dueño de la ficha.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece; siempre el del jugador.</summary>
    public Club? Club { get; set; }

    /// <summary>Contacto de emergencia: nombre. Texto, máx. 160, opcional.</summary>
    public string? EmergenciaNombre { get; set; }

    /// <summary>Contacto de emergencia: parentesco. Máx. 40, opcional.</summary>
    public string? EmergenciaParentesco { get; set; }

    /// <summary>Contacto de emergencia: celular. Máx. 20, opcional.</summary>
    public string? EmergenciaCelular { get; set; }

    /// <summary>Entidad de salud a la que está afiliado. Máx. 120, opcional.</summary>
    public string? EntidadSalud { get; set; }

    /// <summary>Lugar donde lo atienden. Máx. 200, opcional.</summary>
    public string? LugarAtencion { get; set; }

    /// <summary>Grupo sanguíneo. Opcional, se guarda como texto.</summary>
    public GrupoSanguineo? GrupoSanguineo { get; set; }

    /// <summary>Alergias. Máx. 1000, opcional.</summary>
    public string? Alergias { get; set; }

    /// <summary>Enfermedades o condiciones. Máx. 1000, opcional.</summary>
    public string? Enfermedades { get; set; }

    /// <summary>Medicamentos. Máx. 1000, opcional.</summary>
    public string? Medicamentos { get; set; }

    /// <summary>Observaciones. Máx. 1000, opcional.</summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Fecha y hora UTC del último cambio. Obligatorio: la fila solo existe desde el primer cambio.
    /// Los tres datos del último cambio se escriben juntos.
    /// </summary>
    public DateTime UltimoCambioEn { get; set; }

    /// <summary>
    /// Opcional. Cuenta de quien hizo el último cambio. Foránea a <see cref="Usuario"/>; pasa a
    /// nulo si esa cuenta se elimina.
    /// </summary>
    public Guid? UltimoCambioPorUsuarioId { get; set; }

    /// <summary>
    /// Máx. 161, obligatorio. Nombres y apellidos de quien cambió, copiados en ese momento (§13).
    /// </summary>
    public string UltimoCambioPorNombre { get; set; } = string.Empty;
}
