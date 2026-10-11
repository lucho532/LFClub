using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a las categorías del club de la petición con Entity Framework.
/// Su responsabilidad es leerlas con sus equipos y asignaciones activos, contar sus jugadores y
/// cambiarlas con sentencias directas. Trabaja siempre con el filtro de aislamiento activo: sin
/// club en el contexto no devuelve ni cambia nada.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioCategorias : IRepositorioCategorias
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioCategorias(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    private IQueryable<Categoria> Completas => _contexto.Categorias
        .AsNoTrackingWithIdentityResolution()
        .AsSplitQuery()
        .Include(categoria => categoria.Equipos.Where(equipo => equipo.Activo))
            .ThenInclude(equipo => equipo.Jugadores)
        .Include(categoria => categoria.Asignaciones.Where(asignacion => asignacion.Activa))
            .ThenInclude(asignacion => asignacion.UsuarioRol)
        .Include(categoria => categoria.Asignaciones.Where(asignacion => asignacion.Activa))
            .ThenInclude(asignacion => asignacion.Equipos);

    private IQueryable<UsuarioRol> Jugadores => _contexto.UsuariosRol.Where(RepositorioJugadores.EsJugadorDelClub);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken cancelacion = default) =>
        await Completas.OrderBy(categoria => categoria.Anio).ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<Categoria?> ObtenerAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        Completas.FirstOrDefaultAsync(categoria => categoria.Id == categoriaId, cancelacion);

    /// <inheritdoc />
    public Task<Categoria?> ObtenerConCuentasDeEntrenadoresAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        Completas
            .Include(categoria => categoria.Asignaciones.Where(asignacion => asignacion.Activa))
                .ThenInclude(asignacion => asignacion.UsuarioRol!)
                .ThenInclude(integrante => integrante.Usuario)
            .FirstOrDefaultAsync(categoria => categoria.Id == categoriaId, cancelacion);

    /// <inheritdoc />
    public Task<Categoria?> BuscarPorAnioAsync(int anio, CancellationToken cancelacion = default) =>
        _contexto.Categorias.AsNoTracking().FirstOrDefaultAsync(categoria => categoria.Anio == anio, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> ContarJugadoresAsync(CancellationToken cancelacion = default) =>
        await Jugadores
            .Where(jugador => jugador.CategoriaId != null)
            .GroupBy(jugador => jugador.CategoriaId!.Value)
            .Select(grupo => new { CategoriaId = grupo.Key, Numero = grupo.Count() })
            .ToDictionaryAsync(fila => fila.CategoriaId, fila => fila.Numero, cancelacion);

    /// <inheritdoc />
    public Task<int> ContarJugadoresAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        Jugadores.CountAsync(jugador => jugador.CategoriaId == categoriaId, cancelacion);

    /// <inheritdoc />
    public async Task AgregarAsync(Categoria categoria, CancellationToken cancelacion = default)
    {
        _contexto.Categorias.Add(categoria);
        await _contexto.SaveChangesAsync(cancelacion);
        _contexto.Entry(categoria).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public Task CambiarActivaAsync(Guid categoriaId, bool activa, CancellationToken cancelacion = default) =>
        _contexto.Categorias
            .Where(categoria => categoria.Id == categoriaId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(categoria => categoria.Activa, activa), cancelacion);

    /// <inheritdoc />
    public Task<int> ContarActivasAsync(IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default) =>
        _contexto.Categorias.CountAsync(
            categoria => categoriaIds.Contains(categoria.Id) && categoria.Activa, cancelacion);

    /// <inheritdoc />
    public Task MarcarUsadaAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        _contexto.Categorias
            .Where(categoria => categoria.Id == categoriaId && !categoria.Usada)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(categoria => categoria.Usada, true), cancelacion);

    /// <inheritdoc />
    public async Task RetirarEntrenadoresAsync(Guid categoriaId, CancellationToken cancelacion = default)
    {
        await _contexto.EntrenadoresEquipo
            .Where(fila => fila.Asignacion!.CategoriaId == categoriaId)
            .ExecuteDeleteAsync(cancelacion);

        await _contexto.AsignacionesEntrenadorCategoria
            .Where(asignacion => asignacion.CategoriaId == categoriaId && asignacion.Activa)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(asignacion => asignacion.Activa, false), cancelacion);
    }

    /// <inheritdoc />
    public async Task<bool> BorrarSiNuncaSeUsoAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        await _contexto.Categorias
            .Where(categoria => categoria.Id == categoriaId && !categoria.Usada)
            .ExecuteDeleteAsync(cancelacion) == 1;
}
