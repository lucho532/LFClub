using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de aprobar un ingreso en espera (constitución §12.1.1; RF-016).
/// Su responsabilidad es dejar a la persona aprobada como JUGADOR, su único rol en el club,
/// registrar quién la aprobó y cuándo, una sola vez, y ubicarla en la categoría activa de su año
/// de nacimiento, si el club la tiene, igual que a quien entra con una invitación de JUGADOR.
/// No deja elegir rol: una aprobación que indique otro se niega. No asigna equipo, no genera cobros
/// y no envía correos. No cambia el rol de quien ya está aprobado ni decide quién aprueba: lo
/// comprueba la autorización, que solo admite al PRESIDENTE.
/// </summary>
public interface IServicioAprobacionIngreso
{
    /// <summary>
    /// Aprueba el ingreso como JUGADOR. <paramref name="datos"/> puede no venir; si indica un rol
    /// distinto de JUGADOR: 403 <c>rol_no_asignable</c> y la persona sigue en espera.
    /// <paramref name="quienAprueba"/> es el integrante de la sesión en ese club, ya comprobado por
    /// la autorización.
    /// </summary>
    Task<IngresoAprobadoDto> AprobarAsync(
        Guid usuarioRolId, AprobarIngresoDto? datos, UsuarioRol quienAprueba, CancellationToken cancelacion = default);
}
