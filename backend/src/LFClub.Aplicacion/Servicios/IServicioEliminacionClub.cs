using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de eliminar un club (constitución §7.4, RF-029).
/// Su responsabilidad es borrar, de forma irreversible, un club dado de baja con toda su
/// información, y las cuentas que solo pertenecían a él.
/// No elimina un club activo ni suspendido, no toca ningún otro club y nunca elimina la cuenta
/// DESARROLLADOR. Es la eliminación física que §7.4 autoriza expresamente.
/// </summary>
public interface IServicioEliminacionClub
{
    /// <summary>
    /// Elimina el club. Si no está dado de baja: 409 <c>club_no_dado_de_baja</c>. Si el nombre de
    /// confirmación no coincide: 400 <c>confirmacion_no_coincide</c>.
    /// </summary>
    Task EliminarAsync(Guid clubId, EliminarClubDto datos, CancellationToken cancelacion = default);
}
