namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado de una invitación tal como lo ve quien la envió (RF-005).
/// Su responsabilidad es nombrar los cuatro estados que muestra la lista de invitaciones del club.
/// No se guarda: se deriva de las fechas de la invitación en cada consulta, y no describe si el
/// correo llegó a enviarse (eso es <see cref="EstadoEnvio"/>).
/// </summary>
public enum EstadoInvitacion
{
    /// <summary>Sin usar, sin cancelar y antes de su vencimiento: su enlace sirve.</summary>
    PENDIENTE,

    /// <summary>Alguien se registró o aceptó con ella.</summary>
    USADA,

    /// <summary>Pasó su fecha de vencimiento sin usarse ni cancelarse.</summary>
    VENCIDA,

    /// <summary>Quien invita la canceló, o la reemplazó otra más reciente al mismo correo.</summary>
    CANCELADA,
}
