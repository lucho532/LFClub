using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a las invitaciones del club de la petición con Entity Framework.
/// Su responsabilidad es leer y guardar las invitaciones que envía el club. Trabaja siempre con el
/// filtro de aislamiento activo, así que sin club en el contexto no devuelve ni cambia nada, y
/// añade en toda consulta que el rol no sea PRESIDENTE (research §2).
/// No se salta el filtro de aislamiento, no ve invitaciones de presidente y no contiene reglas de
/// negocio.
/// </summary>
public class RepositorioInvitacionesClub : IRepositorioInvitacionesClub
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioInvitacionesClub(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    private IQueryable<Invitacion> DelClub => _contexto.Invitaciones
        .Where(invitacion => invitacion.Rol != Rol.PRESIDENTE);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Invitacion>> ListarLaMasRecientePorCorreoAsync(
        CancellationToken cancelacion = default)
    {
        // Un club tiene decenas de personas (§19): se agrupan en memoria, sin paginar.
        var todas = await DelClub.AsNoTracking().ToListAsync(cancelacion);

        return todas
            .GroupBy(invitacion => invitacion.Correo)
            .Select(delCorreo => delCorreo
                .OrderByDescending(invitacion => invitacion.CreadaEn)
                .ThenByDescending(invitacion => invitacion.Id)
                .First())
            .OrderByDescending(invitacion => invitacion.CreadaEn)
            .ThenByDescending(invitacion => invitacion.Id)
            .ToList();
    }

    /// <inheritdoc />
    public Task<Invitacion?> ObtenerAsync(Guid invitacionId, CancellationToken cancelacion = default) =>
        DelClub.FirstOrDefaultAsync(invitacion => invitacion.Id == invitacionId, cancelacion);

    /// <inheritdoc />
    public Task AnularPendientesAsync(
        string correoNormalizado, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        DelClub
            .Where(invitacion => invitacion.Correo == correoNormalizado
                && invitacion.UsadaEn == null
                && invitacion.AnuladaEn == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(invitacion => invitacion.AnuladaEn, ahoraUtc), cancelacion);

    /// <inheritdoc />
    public void Agregar(Invitacion invitacion) => _contexto.Invitaciones.Add(invitacion);

    /// <inheritdoc />
    public Task<EstadoIngreso?> EstadoDeIngresoDelCorreoAsync(
        string correoNormalizado, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .Where(integrante => integrante.Usuario!.CorreoNormalizado == correoNormalizado)
            .OrderBy(integrante => integrante.CreadoEn)
            .Select(integrante => (EstadoIngreso?)integrante.EstadoIngreso)
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public Task<bool> EsDeUnRetiradoAsync(string correoNormalizado, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol.AnyAsync(
            integrante => integrante.Usuario!.CorreoNormalizado == correoNormalizado && !integrante.Activo,
            cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> NombresDeIntegrantesAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken cancelacion = default)
    {
        var integrantes = await _contexto.UsuariosRol
            .AsNoTracking()
            .Where(integrante => usuarioIds.Contains(integrante.UsuarioId))
            .OrderBy(integrante => integrante.CreadoEn)
            .Select(integrante => new { integrante.UsuarioId, integrante.Nombres, integrante.Apellidos })
            .ToListAsync(cancelacion);

        return integrantes
            .GroupBy(integrante => integrante.UsuarioId)
            .ToDictionary(grupo => grupo.Key, grupo => $"{grupo.First().Nombres} {grupo.First().Apellidos}");
    }

    /// <inheritdoc />
    public Task BorrarDelCorreoAsync(string correoNormalizado, CancellationToken cancelacion = default) =>
        DelClub.Where(invitacion => invitacion.Correo == correoNormalizado).ExecuteDeleteAsync(cancelacion);
}
