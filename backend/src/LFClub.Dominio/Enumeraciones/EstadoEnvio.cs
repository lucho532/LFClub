namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el resultado del envío del correo de una invitación.
/// Su responsabilidad es indicar si el correo salió.
/// No indica si la invitación sigue vigente: eso se deriva de sus fechas.
/// </summary>
public enum EstadoEnvio
{
    /// <summary>Todavía no se ha intentado enviar.</summary>
    PENDIENTE,

    /// <summary>El servicio de correo aceptó el envío.</summary>
    ENVIADO,

    /// <summary>El envío falló; el panel permite reenviarla.</summary>
    FALLIDO,
}
