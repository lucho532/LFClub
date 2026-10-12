using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de crear un club (constitución §8 y §12.5, RF-006 y RF-011).
/// Su responsabilidad es crear el club y la invitación de su presidente en una sola transacción:
/// no existe un club sin presidente invitado.
/// No crea la cuenta del presidente ni configura el escudo o los colores.
/// </summary>
public interface IServicioCreacionClub
{
    /// <summary>
    /// Crea el club e invita a su presidente. Nombre repetido: 409 <c>nombre_de_club_repetido</c>.
    /// Correo de la cuenta DESARROLLADOR: 409 <c>correo_del_desarrollador</c>. Si el correo no se
    /// puede enviar, el club se crea igualmente y la invitación queda como fallida.
    /// </summary>
    Task<ClubDetalleDto> CrearAsync(CrearClubDto datos, Guid usuarioId, CancellationToken cancelacion = default);
}
