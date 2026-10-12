using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla del último presidente (constitución §8, RF-019a y RF-020): un club conserva
/// siempre al menos un PRESIDENTE registrado.
/// Su responsabilidad es responder si a un integrante se le puede quitar el rol o eliminar del
/// club. Vale igual cuando lo decide el DESARROLLADOR que cuando un presidente intenta quitarse su
/// propio rol o eliminar su cuenta.
/// No cuenta las invitaciones pendientes como presidentes ni consulta la base de datos: recibe
/// cuántos presidentes registrados tiene el club, contados dentro de la misma transacción.
/// </summary>
public static class ReglaUltimoPresidente
{
    /// <summary>
    /// Indica si el integrante con ese rol puede perderlo o salir del club, dado el número de
    /// presidentes registrados que tiene el club en este momento (incluido él, si lo es).
    /// </summary>
    public static bool PuedeDejarElRol(Rol rolDelIntegrante, int presidentesRegistradosDelClub) =>
        rolDelIntegrante != Rol.PRESIDENTE || presidentesRegistradosDelClub > 1;
}
