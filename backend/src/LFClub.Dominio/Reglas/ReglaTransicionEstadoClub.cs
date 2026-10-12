using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de las transiciones de estado de un club (constitución §7.4, RF-026 a
/// RF-029).
/// Su responsabilidad es decir qué cambios de estado se permiten (suspender, levantar la
/// suspensión, dar de baja y revertir la baja) y desde qué estado se puede eliminar un club.
/// No cambia el estado, no comprueba quién lo pide y no conoce HTTP.
/// </summary>
public static class ReglaTransicionEstadoClub
{
    /// <summary>
    /// Indica si el club puede pasar de un estado a otro. Pasar al mismo estado no es una
    /// transición y se rechaza.
    /// </summary>
    public static bool SePermite(EstadoClub desde, EstadoClub hacia) => (desde, hacia) switch
    {
        (EstadoClub.ACTIVO, EstadoClub.SUSPENDIDO) => true,
        (EstadoClub.SUSPENDIDO, EstadoClub.ACTIVO) => true,
        (EstadoClub.ACTIVO, EstadoClub.DADO_DE_BAJA) => true,
        (EstadoClub.SUSPENDIDO, EstadoClub.DADO_DE_BAJA) => true,
        (EstadoClub.DADO_DE_BAJA, EstadoClub.ACTIVO) => true,
        _ => false,
    };

    /// <summary>Solo se puede eliminar un club que ya está dado de baja (RF-029).</summary>
    public static bool SePuedeEliminar(EstadoClub estado) => estado == EstadoClub.DADO_DE_BAJA;
}
