using LFClub.Dominio.Entidades;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de la eliminación de una cuenta frente al presidente único (constitución §8 y
/// §12.4, spec 014): una cuenta no se elimina si en algún club es el único PRESIDENTE registrado.
/// Su responsabilidad es señalar los clubes que lo impiden, apoyándose en
/// <see cref="ReglaUltimoPresidente"/> para que el retiro de un rol y la eliminación de la cuenta
/// nunca discrepen.
/// No consulta la base de datos: recibe los integrantes de la cuenta y cuántos presidentes
/// registrados tiene cada club, contados por quien la llama. No cuenta invitaciones pendientes.
/// </summary>
public static class ReglaEliminacionDeCuenta
{
    /// <summary>
    /// Identificadores de los clubes donde la cuenta es PRESIDENTE y no hay otro, en el orden en que
    /// aparecen sus integrantes y sin repetir. Un club ausente de <paramref name="presidentesPorClub"/>
    /// se cuenta como si tuviera un solo presidente: ante la duda, la cuenta no se elimina.
    /// </summary>
    public static IReadOnlyList<Guid> ClubesBloqueantes(
        IEnumerable<UsuarioRol> integrantes, IReadOnlyDictionary<Guid, int> presidentesPorClub) =>
        integrantes
            .Where(integrante => !ReglaUltimoPresidente.PuedeDejarElRol(
                integrante.Rol,
                presidentesPorClub.GetValueOrDefault(integrante.ClubId, 1)))
            .Select(integrante => integrante.ClubId)
            .Distinct()
            .ToList();
}
