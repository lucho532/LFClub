using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los comunicados del club de la petición con Entity Framework (spec 011).
/// Su responsabilidad es resolver los destinatarios en una sola consulta, guardar el comunicado con
/// sus destinos y destinatarios, y listarlos para el equipo del club con cursor. La vista de la
/// cuenta de un jugador (recibidos, contador y lectura) está en <c>RepositorioComunicados.Recibidos.cs</c>.
/// Trabaja siempre con el filtro de aislamiento activo.
/// No decide quién ve qué más allá de lo que pide cada método, no borra nada y no envía avisos.
/// </summary>
public partial class RepositorioComunicados : IRepositorioComunicados
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioComunicados(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DestinatarioDeComunicado>> ResolverDestinatariosAsync(
        AlcanceComunicado alcance,
        IReadOnlyCollection<Guid> categoriaIds,
        IReadOnlyCollection<Guid> equipoIds,
        CancellationToken cancelacion = default)
    {
        var jugadores = _contexto.UsuariosRol.AsNoTracking().Where(integrante =>
            integrante.Rol == Rol.JUGADOR
            && integrante.EstadoIngreso == EstadoIngreso.APROBADO
            && integrante.Activo
            && integrante.RetiradoEn == null);

        var categorias = categoriaIds.ToList();
        var equipos = equipoIds.ToList();
        jugadores = alcance switch
        {
            AlcanceComunicado.CATEGORIAS => jugadores.Where(
                integrante => integrante.CategoriaId != null && categorias.Contains(integrante.CategoriaId.Value)),
            AlcanceComunicado.EQUIPOS => jugadores.Where(integrante => _contexto.JugadoresEquipo.Any(
                fila => fila.UsuarioRolId == integrante.Id && equipos.Contains(fila.EquipoId))),
            _ => jugadores,
        };

        return await jugadores
            .Select(integrante => new DestinatarioDeComunicado(integrante.Id, integrante.UsuarioId))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoriaDeAudiencia>> CategoriasActivasAsync(
        IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default)
    {
        var ids = categoriaIds.ToList();
        return await _contexto.Categorias.AsNoTracking()
            .Where(categoria => ids.Contains(categoria.Id) && categoria.Activa)
            .Select(categoria => new CategoriaDeAudiencia(categoria.Id, categoria.Anio))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipoDeAudiencia>> EquiposActivosAsync(
        IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default)
    {
        var ids = equipoIds.ToList();
        return await _contexto.Equipos.AsNoTracking()
            .Where(equipo => ids.Contains(equipo.Id) && equipo.Activo && equipo.Categoria!.Activa)
            .Select(equipo => new EquipoDeAudiencia(equipo.Id, equipo.Nombre, equipo.CategoriaId, equipo.Categoria!.Anio))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public Task<Comunicado?> BuscarPorClaveAsync(Guid clave, CancellationToken cancelacion = default) =>
        _contexto.Comunicados.AsNoTracking().Include(comunicado => comunicado.Destinos)
            .FirstOrDefaultAsync(comunicado => comunicado.Clave == clave, cancelacion);

    /// <inheritdoc />
    public Task<Comunicado?> ObtenerAsync(Guid comunicadoId, CancellationToken cancelacion = default) =>
        _contexto.Comunicados.AsNoTracking().Include(comunicado => comunicado.Destinos)
            .FirstOrDefaultAsync(comunicado => comunicado.Id == comunicadoId, cancelacion);

    /// <inheritdoc />
    public async Task AgregarAsync(Comunicado comunicado, CancellationToken cancelacion = default)
    {
        _contexto.Comunicados.Add(comunicado);
        try
        {
            await _contexto.SaveChangesAsync(cancelacion);
        }
        finally
        {
            // Que una violación de la clave no deje entidades pendientes en el contexto del ámbito.
            // Se recorre una copia: soltar una fila la quita de la colección del comunicado.
            foreach (var fila in comunicado.Destinatarios.ToList())
            {
                _contexto.Entry(fila).State = EntityState.Detached;
            }

            foreach (var fila in comunicado.Destinos.ToList())
            {
                _contexto.Entry(fila).State = EntityState.Detached;
            }

            _contexto.Entry(comunicado).State = EntityState.Detached;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Comunicado>> ListarPublicadosAsync(
        Guid? autorId, Guid? antesDe, int limite, CancellationToken cancelacion = default)
    {
        var consulta = _contexto.Comunicados.AsNoTracking().Include(comunicado => comunicado.Destinos).AsQueryable();
        if (autorId is { } autor)
        {
            consulta = consulta.Where(comunicado => comunicado.PublicadoPorUsuarioRolId == autor);
        }

        if (antesDe is { } cursor)
        {
            consulta = consulta.Where(comunicado => comunicado.Id.CompareTo(cursor) < 0);
        }

        return await consulta.OrderByDescending(comunicado => comunicado.Id).Take(limite).ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> ContarFamiliasAsync(
        IReadOnlyCollection<Guid> comunicadoIds, CancellationToken cancelacion = default)
    {
        var ids = comunicadoIds.ToList();
        return await _contexto.ComunicadosDestinatario.AsNoTracking()
            .Where(fila => ids.Contains(fila.ComunicadoId))
            .GroupBy(fila => fila.ComunicadoId)
            .Select(grupo => new { Id = grupo.Key, Cuentas = grupo.Select(fila => fila.UsuarioId).Distinct().Count() })
            .ToDictionaryAsync(fila => fila.Id, fila => fila.Cuentas, cancelacion);
    }

    /// <inheritdoc />
    public Task RetirarAsync(
        Guid comunicadoId, Guid porUsuarioRolId, string porNombre, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        _contexto.Comunicados
            .Where(comunicado => comunicado.Id == comunicadoId && comunicado.RetiradoEn == null)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(comunicado => comunicado.RetiradoEn, ahoraUtc)
                    .SetProperty(comunicado => comunicado.RetiradoPorUsuarioRolId, porUsuarioRolId)
                    .SetProperty(comunicado => comunicado.RetiradoPorNombre, porNombre),
                cancelacion);
}
