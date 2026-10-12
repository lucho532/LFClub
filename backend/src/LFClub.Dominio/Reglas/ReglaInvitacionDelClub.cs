using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de qué roles admite una invitación enviada desde el club (constitución §8 y
/// §12.1; RF-001 y RF-002).
/// Su responsabilidad es responder con qué rol puede entrar una persona invitada por el club:
/// JUGADOR, ENTRENADOR o DIRECTIVO. Nunca PRESIDENTE, que solo lo pone la creación de un club, ni
/// DESARROLLADOR, que no pertenece a ningún club.
/// No decide quién puede invitar: eso lo comprueba la autorización, que solo admite al PRESIDENTE.
/// No conoce HTTP ni la base de datos.
/// </summary>
public static class ReglaInvitacionDelClub
{
    /// <summary>Roles con los que el club puede invitar.</summary>
    public static IReadOnlyList<Rol> RolesInvitables { get; } = [Rol.JUGADOR, Rol.ENTRENADOR, Rol.DIRECTIVO];

    /// <summary>Indica si una invitación del club puede llevar ese rol.</summary>
    public static bool Admite(Rol rol) => RolesInvitables.Contains(rol);
}
