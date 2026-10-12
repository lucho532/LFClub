using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de recuperar la contraseña por correo (constitución §12.4, RF-005a).
/// Su responsabilidad es enviar un enlace de un solo uso y, con él, crear una contraseña nueva que
/// además desbloquea la cuenta y cierra las sesiones anteriores.
/// No revela si un correo tiene cuenta y nadie asigna la contraseña de otra persona.
/// </summary>
public interface IServicioRecuperacion
{
    /// <summary>Pide el correo de recuperación. No falla ni avisa si el correo no existe.</summary>
    Task PedirAsync(PedirRecuperacionDto datos, CancellationToken cancelacion = default);

    /// <summary>
    /// Crea la contraseña nueva. Enlace usado, caducado o inexistente: 410 <c>enlace_no_valido</c>.
    /// </summary>
    Task ConfirmarAsync(RestablecerContrasenaDto datos, CancellationToken cancelacion = default);
}
