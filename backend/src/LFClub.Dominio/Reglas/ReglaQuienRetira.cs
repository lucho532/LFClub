using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién puede retirar del club a un jugador (constitución §14.1; research
/// §18 de la 006).
/// Su responsabilidad es decidir, a partir del integrante que hace la petición, si puede retirar a
/// ese jugador: el PRESIDENTE, a cualquiera de su club; la cuenta de un jugador, solo al jugador de
/// la petición (el elegido o el de la sesión iniciada con su documento), no a un hermano ni a un
/// jugador ajeno; cualquier otro rol, a nadie.
/// No mira el estado del jugador que se retira ni si existe: eso lo comprueba el servicio después.
/// No lee datos ni conoce HTTP, y no decide quién reincorpora (solo el PRESIDENTE, por la
/// autorización).
/// </summary>
public static class ReglaQuienRetira
{
    /// <summary>Indica si <paramref name="quienRetira"/> puede retirar al jugador <paramref name="usuarioRolId"/>.</summary>
    public static bool PuedeRetirar(UsuarioRol quienRetira, Guid usuarioRolId) => quienRetira.Rol switch
    {
        Rol.PRESIDENTE => true,
        Rol.JUGADOR => usuarioRolId == quienRetira.Id,
        _ => false,
    };
}
