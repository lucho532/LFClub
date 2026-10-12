namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa la cuenta con la que una persona inicia sesión. Es una sola aunque la persona
/// pertenezca a varios clubes.
/// Su responsabilidad es guardar el correo, el hash de la contraseña, los datos de contacto
/// (celular y responsable), el bloqueo por intentos fallidos y el sello que invalida las sesiones
/// anteriores.
/// No guarda el club, el rol, el nombre ni el documento: eso vive en <see cref="UsuarioRol"/>.
/// </summary>
public class Usuario
{
    /// <summary>Número de fallos seguidos que bloquea la cuenta (RF-005).</summary>
    public const int IntentosParaBloquear = 5;

    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Correo tal como se muestra. Obligatorio.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Correo en minúsculas y sin espacios en los extremos. Único (RF-002).</summary>
    public string CorreoNormalizado { get; set; } = string.Empty;

    /// <summary>
    /// Hash de la contraseña. Nulo solo en la cuenta DESARROLLADOR recién creada. Nunca sale por
    /// la API (RF-019).
    /// </summary>
    public string? ContrasenaHash { get; set; }

    /// <summary>
    /// Celular de contacto. Obligatorio al registrarse. Después lo cambian, desde la ficha de un
    /// jugador de la cuenta, la propia cuenta y el PRESIDENTE del club de ese jugador; como es de
    /// la cuenta, el cambio se ve en todos sus jugadores y clubes (RF-039 de la 005). Un PRESIDENTE,
    /// DIRECTIVO o ENTRENADOR también lo cambia desde "Mi perfil" (spec 008).
    /// </summary>
    public string? Celular { get; set; }

    /// <summary>
    /// Opcional. Nombre del padre, madre o responsable. Se pide al registrarse con una invitación
    /// de JUGADOR: obligatorio si la persona es menor de 18 años ese día y opcional si es adulta;
    /// con cualquier otro rol queda nulo (RF-013). Una cuenta que ya existe sin él lo recibe al
    /// aceptar una invitación de JUGADOR siendo menor (RF-026). Después se cambia desde la ficha de
    /// un jugador de la cuenta, igual que el celular, y sigue siendo obligatorio mientras ese
    /// jugador sea menor (RF-020 de la 005). Es un dato de contacto de la cuenta: sale por la API
    /// en la sala de espera del club y en la ficha de sus jugadores.
    /// </summary>
    public string? NombreResponsable { get; set; }

    /// <summary>Marca la única cuenta DESARROLLADOR de la plataforma (RF-001).</summary>
    public bool EsDesarrollador { get; set; }

    /// <summary>Fallos seguidos de inicio de sesión, de 0 a 5.</summary>
    public int IntentosFallidos { get; set; }

    /// <summary>Verdadero desde el quinto fallo seguido hasta recuperar la contraseña.</summary>
    public bool Bloqueada { get; set; }

    /// <summary>Cambia al cambiar la contraseña; invalida los tokens anteriores.</summary>
    public Guid SelloSeguridad { get; set; } = Guid.NewGuid();

    /// <summary>Sube cada vez que cambia la foto de perfil; 0 significa que no tiene.</summary>
    public int VersionFoto { get; set; }

    /// <summary>Fecha de creación, en UTC.</summary>
    public DateTime CreadoEn { get; set; }

    /// <summary>Indica si la cuenta puede iniciar sesión: tiene contraseña y no está bloqueada.</summary>
    public bool PuedeIniciarSesion => ContrasenaHash is not null && !Bloqueada;
}
