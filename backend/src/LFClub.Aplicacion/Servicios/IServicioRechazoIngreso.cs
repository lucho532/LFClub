namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de rechazar un ingreso en espera (constitución §12.1.1 y §14; RF-018).
/// Su responsabilidad es borrar a esa persona del club sin dejar datos suyos en él: su integrante,
/// las invitaciones del club a su correo y, si ese era su único club, su cuenta.
/// No rechaza a un integrante ya aprobado, no toca sus otros clubes, no envía ningún correo ni
/// aviso y no deja registro del rechazo. No decide quién puede rechazar: lo comprueba la
/// autorización, que solo admite al PRESIDENTE.
/// </summary>
public interface IServicioRechazoIngreso
{
    /// <summary>Rechaza el ingreso en espera de ese integrante del club de la petición.</summary>
    Task RechazarAsync(Guid usuarioRolId, CancellationToken cancelacion = default);
}
