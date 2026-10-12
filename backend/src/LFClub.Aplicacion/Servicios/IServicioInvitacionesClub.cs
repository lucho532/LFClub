using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de las invitaciones que envía un club (constitución §8 y §12.1; RF-001
/// a RF-004).
/// Su responsabilidad es invitar por correo con el rol que elige el PRESIDENTE (JUGADOR,
/// ENTRENADOR o DIRECTIVO), listar las invitaciones del club con su rol y su estado y reenviar o
/// cancelar las pendientes, siempre dentro del club de la petición.
/// No decide quién puede hacerlo (lo comprueba la autorización, que solo admite al PRESIDENTE), no
/// cambia el rol de una invitación ya creada y ningún método devuelve el token a la API. No toca
/// las invitaciones de presidente.
/// </summary>
public interface IServicioInvitacionesClub
{
    /// <summary>Invitaciones del club, una por correo: la más reciente de cada uno.</summary>
    Task<IReadOnlyList<InvitacionClubDto>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Invita a ese correo con el rol indicado, anulando la invitación pendiente que tuviera: vale
    /// el rol de la nueva. Sin rol: 400 en el campo <c>rol</c>. Con PRESIDENTE o DESARROLLADOR:
    /// 403 <c>rol_no_invitable</c>. Si el correo no se puede enviar, la invitación queda creada
    /// con el envío fallido.
    /// </summary>
    Task<InvitacionClubDto> InvitarAsync(
        InvitarAlClubDto datos, Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>Anula una invitación pendiente y envía otra al mismo correo y con el mismo rol.</summary>
    Task<InvitacionClubDto> ReenviarAsync(Guid invitacionId, Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>Cancela una invitación pendiente: su enlace deja de servir.</summary>
    Task<InvitacionClubDto> CancelarAsync(Guid invitacionId, CancellationToken cancelacion = default);
}
