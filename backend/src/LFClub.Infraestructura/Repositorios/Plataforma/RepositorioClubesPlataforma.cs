using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios.Plataforma;

/// <summary>
/// Representa el acceso a los clubes desde el panel del DESARROLLADOR (constitución §7.1, primera
/// excepción).
/// Su responsabilidad es leer y guardar clubes, sus presidentes y su escudo. Se salta el filtro de
/// aislamiento porque el panel trabaja sobre todos los clubes, y por eso solo consulta
/// integrantes con rol PRESIDENTE o los cuenta por rol, sin leer sus datos personales.
/// No expone ningún otro dato de un club (§8) ni contiene reglas de negocio.
/// </summary>
public class RepositorioClubesPlataforma : IRepositorioClubesPlataforma
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioClubesPlataforma(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    private IQueryable<UsuarioRol> Presidentes => _contexto.UsuariosRol
        .IgnoreQueryFilters()
        .Where(integrante => integrante.Rol == Rol.PRESIDENTE);

    /// <summary>Integrantes de ingreso aprobado y no retirados, de cualquier club.</summary>
    private IQueryable<UsuarioRol> Activos => _contexto.UsuariosRol
        .IgnoreQueryFilters()
        .Where(integrante => integrante.EstadoIngreso == EstadoIngreso.APROBADO && integrante.Activo);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClubConPresidente>> ListarAsync(CancellationToken cancelacion = default) =>
        await _contexto.Clubes
            .OrderBy(club => club.Nombre)
            .Select(club => new ClubConPresidente(
                club,
                Presidentes.Any(integrante => integrante.ClubId == club.Id),
                Activos.Count(integrante => integrante.ClubId == club.Id && integrante.Rol == Rol.JUGADOR),
                Activos.Count(integrante => integrante.ClubId == club.Id && integrante.Rol == Rol.ENTRENADOR),
                Activos.Count(integrante => integrante.ClubId == club.Id && integrante.Rol == Rol.DIRECTIVO),
                Activos.Count(integrante => integrante.ClubId == club.Id && integrante.Rol == Rol.PRESIDENTE)))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<Club?> ObtenerAsync(Guid clubId, CancellationToken cancelacion = default) =>
        _contexto.Clubes.FirstOrDefaultAsync(club => club.Id == clubId, cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(
        string nombreNormalizado, Guid? exceptoClubId = null, CancellationToken cancelacion = default) =>
        _contexto.Clubes.AnyAsync(
            club => club.NombreNormalizado == nombreNormalizado && club.Id != exceptoClubId, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarPresidentesAsync(
        Guid clubId, CancellationToken cancelacion = default) =>
        await Presidentes
            .Include(integrante => integrante.Usuario)
            .Where(integrante => integrante.ClubId == clubId)
            .OrderBy(integrante => integrante.CreadoEn)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<Club?> ObtenerBloqueandoAsync(Guid clubId, CancellationToken cancelacion = default) =>
        _contexto.Clubes
            .FromSqlInterpolated($"SELECT * FROM \"Clubes\" WHERE \"Id\" = {clubId} FOR UPDATE")
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public Task<UsuarioRol?> ObtenerPresidenteAsync(
        Guid clubId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        Presidentes.FirstOrDefaultAsync(
            integrante => integrante.ClubId == clubId && integrante.Id == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public Task<int> ContarPresidentesAsync(Guid clubId, CancellationToken cancelacion = default) =>
        Presidentes.CountAsync(integrante => integrante.ClubId == clubId, cancelacion);

    /// <inheritdoc />
    public void EliminarIntegrante(UsuarioRol integrante) => _contexto.UsuariosRol.Remove(integrante);

    /// <inheritdoc />
    public async Task GuardarEscudoAsync(
        Guid clubId, byte[] contenido, string tipoContenido, CancellationToken cancelacion = default)
    {
        var escudo = await _contexto.EscudosClub
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(actual => actual.ClubId == clubId, cancelacion);

        if (escudo is null)
        {
            _contexto.EscudosClub.Add(new EscudoClub { ClubId = clubId, Contenido = contenido, TipoContenido = tipoContenido });
            return;
        }

        escudo.Contenido = contenido;
        escudo.TipoContenido = tipoContenido;
    }

    /// <inheritdoc />
    public async Task EliminarConSusCuentasAsync(Guid clubId, CancellationToken cancelacion = default)
    {
        var cuentasDelClub = await _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .Where(integrante => integrante.ClubId == clubId)
            .Select(integrante => integrante.UsuarioId)
            .Distinct()
            .ToListAsync(cancelacion);

        // La cascada de la base de datos borra integrantes, invitaciones y escudo (research §12).
        await _contexto.Clubes.Where(club => club.Id == clubId).ExecuteDeleteAsync(cancelacion);

        await _contexto.Usuarios
            .Where(usuario => cuentasDelClub.Contains(usuario.Id)
                && !usuario.EsDesarrollador
                && !_contexto.UsuariosRol.IgnoreQueryFilters().Any(integrante => integrante.UsuarioId == usuario.Id))
            .ExecuteDeleteAsync(cancelacion);

        _contexto.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public void Agregar(Club club) => _contexto.Clubes.Add(club);
}
