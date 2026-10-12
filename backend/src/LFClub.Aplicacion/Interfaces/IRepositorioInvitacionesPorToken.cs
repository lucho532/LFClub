using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a una invitación por su enlace (constitución §7.1, tercera excepción).
/// Su responsabilidad es encontrar una invitación solo por el hash de su token, con su club, y
/// marcarla como usada.
/// No lista invitaciones ni permite buscarlas por club o por correo: quien no tiene el token no
/// obtiene nada.
/// </summary>
public interface IRepositorioInvitacionesPorToken
{
    /// <summary>Busca la invitación de ese hash de token, con su club.</summary>
    Task<Invitacion?> ObtenerPorHashAsync(string tokenHash, CancellationToken cancelacion = default);

    /// <summary>
    /// Marca la invitación como usada solo si seguía sin usar y sin anular; devuelve si la marcó.
    /// Así una invitación sirve una sola vez aunque lleguen dos peticiones a la vez.
    /// </summary>
    Task<bool> MarcarUsadaAsync(Guid invitacionId, DateTime ahoraUtc, CancellationToken cancelacion = default);
}
