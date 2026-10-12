using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de aceptar una invitación con una cuenta que ya existe (RF-010).
/// Su responsabilidad es añadir a la cuenta al club de la invitación, con el rol de la invitación
/// y ya aprobada, sin sala de espera y ubicada en su categoría si entra como JUGADOR; o, con una
/// invitación de presidente, reemplazar su rol si ya era integrante de ese club. Nunca crea una
/// segunda cuenta ni un segundo integrante.
/// No sirve para registrarse: exige sesión, y la sesión debe ser la de la cuenta cuyo correo
/// recibió la invitación. No cambia nada en los otros clubes de la persona ni pide ningún dato,
/// salvo el nombre del responsable a un JUGADOR menor de 18 años cuya cuenta no lo tiene (RF-026).
/// </summary>
public interface IServicioAceptacionInvitacion
{
    /// <summary>
    /// Acepta la invitación. Devuelve el club y si se creó una pertenencia nueva (falso cuando la
    /// cuenta ya era integrante y solo cambió su rol). Sesión de otro correo: 403
    /// <c>invitacion_de_otro_correo</c>. Invitación no vigente: 410 <c>invitacion_no_valida</c>.
    /// JUGADOR menor sin responsable en la cuenta ni en los datos: 400 <c>datos_invalidos</c>, y la
    /// invitación no se gasta.
    /// </summary>
    Task<(ClubDeSesionDto Club, bool Creada)> AceptarAsync(
        AceptarInvitacionDto datos, Guid usuarioId, CancellationToken cancelacion = default);
}
