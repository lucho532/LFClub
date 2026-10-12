using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios.Plataforma;

/// <summary>
/// Representa el acceso a las invitaciones desde el panel del DESARROLLADOR (constitución §7.1,
/// primera excepción).
/// Su responsabilidad es guardar, listar y anular las invitaciones de presidente de un club. Se
/// salta el filtro de aislamiento porque el panel no tiene un club en el contexto; cada consulta
/// va acotada al club indicado y al rol PRESIDENTE.
/// No busca invitaciones por token ni contiene reglas de negocio, y no ve, lista ni anula las
/// invitaciones que envía el propio club (RF-029).
/// </summary>
public class RepositorioInvitacionesPlataforma : IRepositorioInvitacionesPlataforma
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioInvitacionesPlataforma(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    // Solo las de presidente: las que envía el club no son del panel (RF-029).
    private IQueryable<Invitacion> DePresidente(Guid clubId) => _contexto.Invitaciones
        .IgnoreQueryFilters()
        .Where(invitacion => invitacion.ClubId == clubId && invitacion.Rol == Rol.PRESIDENTE);

    private IQueryable<Invitacion> Pendientes(Guid clubId) => DePresidente(clubId)
        .Where(invitacion => invitacion.UsadaEn == null && invitacion.AnuladaEn == null);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Invitacion>> ListarPendientesAsync(
        Guid clubId, CancellationToken cancelacion = default) =>
        await Pendientes(clubId).OrderByDescending(invitacion => invitacion.CreadaEn).ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<Invitacion?> ObtenerAsync(Guid clubId, Guid invitacionId, CancellationToken cancelacion = default) =>
        DePresidente(clubId).FirstOrDefaultAsync(invitacion => invitacion.Id == invitacionId, cancelacion);

    /// <inheritdoc />
    public Task AnularPendientesAsync(
        Guid clubId, string correoNormalizado, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        Pendientes(clubId)
            .Where(invitacion => invitacion.Correo == correoNormalizado)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(invitacion => invitacion.AnuladaEn, ahoraUtc), cancelacion);

    /// <inheritdoc />
    public void Agregar(Invitacion invitacion) => _contexto.Invitaciones.Add(invitacion);
}
