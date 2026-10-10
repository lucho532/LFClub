using System.Linq.Expressions;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los jugadores del club de la petición con Entity Framework.
/// Su responsabilidad es leer las listas de jugadores y al jugador de una ficha, y cambiar su
/// categoría, su retiro, su documento y su identidad con una única sentencia condicionada cada vez. Trabaja siempre con el filtro de aislamiento activo: sin
/// club en el contexto no devuelve ni cambia nada.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio: solo expresa en la consulta
/// qué es un jugador del club.
/// </summary>
public class RepositorioJugadores : IRepositorioJugadores
{
    /// <summary>Jugador del club: rol JUGADOR, ingreso aprobado y no retirado (RF-012).</summary>
    internal static readonly Expression<Func<UsuarioRol, bool>> EsJugadorDelClub = integrante =>
        integrante.Rol == Rol.JUGADOR && integrante.EstadoIngreso == EstadoIngreso.APROBADO && integrante.Activo;

    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioJugadores(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    private IQueryable<UsuarioRol> Jugadores => _contexto.UsuariosRol.Where(EsJugadorDelClub);

    /// <summary>Quien tiene ficha: rol JUGADOR e ingreso aprobado, activo o retirado (RF-004 de la 005).</summary>
    private IQueryable<UsuarioRol> ConFicha => _contexto.UsuariosRol
        .Where(integrante => integrante.Rol == Rol.JUGADOR && integrante.EstadoIngreso == EstadoIngreso.APROBADO);

    /// <inheritdoc />
    public Task<UsuarioRol?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol.AsNoTracking()
            .FirstOrDefaultAsync(integrante => integrante.Id == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public Task<UsuarioRol?> ObtenerConEquiposAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol.AsNoTracking()
            .Include(integrante => integrante.Equipos)
            .FirstOrDefaultAsync(integrante => integrante.Id == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public Task<UsuarioRol?> ObtenerParaFichaAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol.AsNoTracking()
            .Include(integrante => integrante.Usuario)
            .Include(integrante => integrante.Categoria)
            .Include(integrante => integrante.Equipos).ThenInclude(fila => fila.Equipo)
            .FirstOrDefaultAsync(
                integrante => integrante.Id == usuarioRolId
                    && integrante.Rol == Rol.JUGADOR
                    && integrante.EstadoIngreso == EstadoIngreso.APROBADO,
                cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarDelClubAsync(CancellationToken cancelacion = default) =>
        await Jugadores.AsNoTracking()
            .Include(jugador => jugador.Categoria)
            .Include(jugador => jugador.Equipos).ThenInclude(fila => fila.Equipo)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarSinCategoriaAsync(CancellationToken cancelacion = default) =>
        await Jugadores.AsNoTracking().Where(jugador => jugador.CategoriaId == null).ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarDeCategoriaAsync(
        Guid categoriaId, CancellationToken cancelacion = default) =>
        await Jugadores.AsNoTracking()
            .Include(jugador => jugador.Equipos)
            .Where(jugador => jugador.CategoriaId == categoriaId)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarRetiradosAsync(CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol.AsNoTracking()
            .Where(integrante => integrante.Rol == Rol.JUGADOR && !integrante.Activo)
            .OrderByDescending(integrante => integrante.RetiradoEn)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<bool> UbicarSiNoTieneAsync(
        Guid usuarioRolId, Guid categoriaId, CancellationToken cancelacion = default) =>
        await Jugadores
            .Where(jugador => jugador.Id == usuarioRolId && jugador.CategoriaId == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(jugador => jugador.CategoriaId, categoriaId), cancelacion) == 1;

    /// <inheritdoc />
    public Task<int> RecogerDelAnioAsync(Guid categoriaId, int anio, CancellationToken cancelacion = default)
    {
        // El año de nacimiento se compara con su intervalo, sin columna calculada (research §4).
        var primerDia = new DateOnly(anio, 1, 1);
        var ultimoDia = new DateOnly(anio, 12, 31);

        return Jugadores
            .Where(jugador => jugador.CategoriaId == null
                && jugador.FechaNacimiento >= primerDia
                && jugador.FechaNacimiento <= ultimoDia)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(jugador => jugador.CategoriaId, categoriaId), cancelacion);
    }

    /// <inheritdoc />
    public async Task<bool> CambiarCategoriaAsync(
        Guid usuarioRolId, Guid categoriaId, CancellationToken cancelacion = default) =>
        await Jugadores
            .Where(jugador => jugador.Id == usuarioRolId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(jugador => jugador.CategoriaId, categoriaId), cancelacion) == 1;

    /// <inheritdoc />
    public Task SacarDeSusEquiposAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.JugadoresEquipo.Where(fila => fila.UsuarioRolId == usuarioRolId).ExecuteDeleteAsync(cancelacion);

    /// <inheritdoc />
    public async Task<bool> RetirarAsync(
        Guid usuarioRolId,
        DateTime ahoraUtc,
        Guid retiradoPorUsuarioId,
        string retiradoPorNombre,
        CancellationToken cancelacion = default) =>
        await Jugadores
            .Where(jugador => jugador.Id == usuarioRolId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(jugador => jugador.Activo, false)
                    .SetProperty(jugador => jugador.CategoriaId, (Guid?)null)
                    .SetProperty(jugador => jugador.RetiradoEn, ahoraUtc)
                    .SetProperty(jugador => jugador.RetiradoPorUsuarioId, retiradoPorUsuarioId)
                    .SetProperty(jugador => jugador.RetiradoPorNombre, retiradoPorNombre),
                cancelacion) == 1;

    /// <inheritdoc />
    public async Task<bool> ReincorporarAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol
            .Where(integrante => integrante.Id == usuarioRolId && integrante.Rol == Rol.JUGADOR && !integrante.Activo)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(integrante => integrante.Activo, true)
                    .SetProperty(integrante => integrante.RetiradoEn, (DateTime?)null)
                    .SetProperty(integrante => integrante.RetiradoPorUsuarioId, (Guid?)null)
                    .SetProperty(integrante => integrante.RetiradoPorNombre, (string?)null),
                cancelacion) == 1;

    /// <inheritdoc />
    public Task<bool> ExisteDocumentoEnOtroAsync(
        Guid usuarioRolId, string numeroDocumento, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol.AnyAsync(
            integrante => integrante.Id != usuarioRolId && integrante.NumeroDocumento == numeroDocumento, cancelacion);

    /// <inheritdoc />
    public async Task<bool> CambiarDocumentoAsync(
        Guid usuarioRolId, TipoDocumento tipoDocumento, string numeroDocumento, CancellationToken cancelacion = default) =>
        await ConFicha
            .Where(jugador => jugador.Id == usuarioRolId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(jugador => jugador.TipoDocumento, tipoDocumento)
                    .SetProperty(jugador => jugador.NumeroDocumento, numeroDocumento),
                cancelacion) == 1;

    /// <inheritdoc />
    public async Task<bool> CorregirIdentidadAsync(
        Guid usuarioRolId,
        string nombres,
        string apellidos,
        DateOnly fechaNacimiento,
        CancellationToken cancelacion = default) =>
        await ConFicha
            .Where(jugador => jugador.Id == usuarioRolId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(jugador => jugador.Nombres, nombres)
                    .SetProperty(jugador => jugador.Apellidos, apellidos)
                    .SetProperty(jugador => jugador.FechaNacimiento, fechaNacimiento),
                cancelacion) == 1;
}
