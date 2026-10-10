using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios.Plataforma;

/// <summary>
/// Representa el acceso entre clubes de una cuenta y de un número de documento (constitución §7.1,
/// segunda y cuarta excepción).
/// Su responsabilidad es responder a qué clubes pertenece una cuenta, cuáles son sus integrantes
/// en uno de ellos y qué integrantes tienen un número de documento. Por eso se salta el filtro de
/// aislamiento, y cada consulta va acotada por cuenta o por documento.
/// No lista los integrantes de un club ni entrega datos de un club a quien no pertenece a él.
/// </summary>
public class RepositorioPertenencias : IRepositorioPertenencias
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioPertenencias(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarDeUsuarioAsync(
        Guid usuarioId, CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .Include(integrante => integrante.Club)
            .Where(integrante => integrante.UsuarioId == usuarioId)
            .OrderBy(integrante => integrante.Club!.Nombre)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarDeLaCuentaEnClubAsync(
        Guid usuarioId, Guid clubId, CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .Include(integrante => integrante.Club)
            .Where(integrante => integrante.UsuarioId == usuarioId && integrante.ClubId == clubId)
            .OrderBy(integrante => integrante.CreadoEn)
            .ThenBy(integrante => integrante.Id)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<UsuarioRol?> ObtenerAsync(Guid usuarioId, Guid clubId, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .Include(integrante => integrante.Club)
            .Where(integrante => integrante.UsuarioId == usuarioId && integrante.ClubId == clubId)
            .OrderBy(integrante => integrante.CreadoEn)
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public Task<Usuario?> ObtenerCuentaPorDocumentoAsync(
        string numeroDocumento, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .Where(integrante => integrante.NumeroDocumento == numeroDocumento)
            .Select(integrante => integrante.Usuario)
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarPorDocumentosAsync(
        IReadOnlyCollection<string> numerosNormalizados, CancellationToken cancelacion = default)
    {
        if (numerosNormalizados.Count == 0)
        {
            return [];
        }

        return await _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(integrante => numerosNormalizados.Contains(integrante.NumeroDocumento))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> IdsDeLaCuentaConDocumentoAsync(
        Guid usuarioId, string numeroDocumento, CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .Where(integrante => integrante.UsuarioId == usuarioId && integrante.NumeroDocumento == numeroDocumento)
            .Select(integrante => integrante.Id)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<UsuarioRol?> ObtenerPorDocumentoEnClubAsync(
        Guid clubId, string numeroDocumento, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                integrante => integrante.ClubId == clubId && integrante.NumeroDocumento == numeroDocumento, cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteDocumentoEnClubAsync(
        Guid clubId, string numeroDocumento, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .AnyAsync(integrante => integrante.ClubId == clubId && integrante.NumeroDocumento == numeroDocumento, cancelacion);

    /// <inheritdoc />
    public Task<bool> EsDocumentoDeRetiradoAsync(
        Guid clubId, string numeroDocumento, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .AnyAsync(
                integrante => integrante.ClubId == clubId
                    && integrante.NumeroDocumento == numeroDocumento
                    && !integrante.Activo,
                cancelacion);

    /// <inheritdoc />
    public Task SacarDeSusEquiposAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.JugadoresEquipo
            .IgnoreQueryFilters()
            .Where(fila => fila.UsuarioRolId == usuarioRolId)
            .ExecuteDeleteAsync(cancelacion);

    /// <inheritdoc />
    public Task<bool> TieneAlgunaAsync(Guid usuarioId, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .AnyAsync(integrante => integrante.UsuarioId == usuarioId, cancelacion);

    /// <inheritdoc />
    public void Agregar(UsuarioRol integrante) => _contexto.UsuariosRol.Add(integrante);
}
