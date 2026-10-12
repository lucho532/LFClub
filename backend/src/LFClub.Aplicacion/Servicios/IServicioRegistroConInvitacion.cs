using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de registrarse con una invitación (constitución §12.1 y §12.5).
/// Su responsabilidad es mostrar los datos fijos de una invitación vigente y crear, con ella, la
/// cuenta y su integrante en el club y con el rol de la invitación, como único rol y ya aprobado:
/// entra directamente, sin sala de espera (RF-008). A quien entra como JUGADOR lo deja en la
/// categoría activa de su año de nacimiento, si el club la tiene (RF-011).
/// No existe ningún otro camino para crear una cuenta, y nadie elige su club, su rol ni su correo:
/// los determina la invitación. No asigna equipo, no genera cobros y no avisa al club.
/// </summary>
public interface IServicioRegistroConInvitacion
{
    /// <summary>
    /// Devuelve los datos de la invitación, y si la cuenta que ya existe tendrá que dar el nombre
    /// del responsable al aceptarla (RF-026). Usada, vencida, anulada o inexistente: 410
    /// <c>invitacion_no_valida</c>.
    /// </summary>
    Task<InvitacionVigenteDto> ConsultarAsync(TokenDto datos, CancellationToken cancelacion = default);

    /// <summary>
    /// Registra la cuenta y su integrante, marca la invitación como usada e inicia la sesión. El
    /// nombre del responsable solo se exige y se guarda con una invitación de JUGADOR.
    /// </summary>
    Task<TokenSesionDto> RegistrarAsync(RegistrarConInvitacionDto datos, CancellationToken cancelacion = default);
}
