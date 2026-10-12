using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa la construcción de una invitación nueva, igual para la de presidente que envía el
/// DESARROLLADOR y para la que envía un club.
/// Su responsabilidad es generar el token de 32 bytes, guardar solo su hash, fijar el vencimiento a
/// 7 días de la creación y dejar el envío como pendiente.
/// No guarda la invitación, no anula las anteriores y no envía el correo: eso lo hace cada servicio.
/// </summary>
public static class ConstructorInvitaciones
{
    /// <summary>
    /// Crea la invitación y devuelve también su token en claro, que solo sirve para enviarlo por
    /// correo.
    /// </summary>
    public static (Invitacion Invitacion, string Token) Nueva(
        Guid clubId, Rol rol, string correoNormalizado, Guid creadaPorUsuarioId, DateTime ahoraUtc)
    {
        var (token, hash) = GeneradorTokens.Nuevo();
        var invitacion = new Invitacion
        {
            ClubId = clubId,
            Rol = rol,
            Correo = correoNormalizado,
            TokenHash = hash,
            EstadoEnvio = EstadoEnvio.PENDIENTE,
            CreadaPorUsuarioId = creadaPorUsuarioId,
            CreadaEn = ahoraUtc,
            VenceEn = ahoraUtc.AddDays(Invitacion.DiasDeVigencia),
        };

        return (invitacion, token);
    }
}
