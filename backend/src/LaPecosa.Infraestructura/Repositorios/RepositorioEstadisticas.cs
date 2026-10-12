using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el cálculo de las estadísticas del club de la petición con Entity Framework.
/// Su responsabilidad es contar con <c>GROUP BY</c> sobre las filas de convocados y de asistencia,
/// por la categoría copiada en cada una. Trabaja siempre con el filtro de aislamiento activo.
/// No guarda nada ni decide quién ve las cifras.
/// </summary>
public class RepositorioEstadisticas : IRepositorioEstadisticas
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioEstadisticas(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CifrasDeCategoria>> DelJugadorAsync(Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var partidos = await _contexto.PartidosJugador
            .Where(fila => fila.UsuarioRolId == usuarioRolId && fila.Partido!.Estado == EstadoPartido.JUGADO)
            .GroupBy(fila => fila.CategoriaId)
            .Select(grupo => new { CategoriaId = grupo.Key, Jugados = grupo.Count(fila => fila.Jugo), Goles = grupo.Sum(fila => fila.Goles) })
            .ToListAsync(cancelacion);

        var asistencias = await _contexto.AsistenciasEntrenamiento
            .Where(fila => fila.UsuarioRolId == usuarioRolId && fila.Entrenamiento!.Estado != EstadoEntrenamiento.CANCELADO)
            .GroupBy(fila => fila.CategoriaId)
            .Select(grupo => new { CategoriaId = grupo.Key, Registrados = grupo.Count(), Asistidos = grupo.Count(fila => fila.Presente) })
            .ToListAsync(cancelacion);

        var ids = partidos.Select(fila => fila.CategoriaId).Union(asistencias.Select(fila => fila.CategoriaId)).ToList();
        var categorias = await _contexto.Categorias.AsNoTracking()
            .Where(categoria => ids.Contains(categoria.Id))
            .ToDictionaryAsync(categoria => categoria.Id, cancelacion);

        return ids
            .Where(categorias.ContainsKey)
            .Select(id =>
            {
                var dePartidos = partidos.FirstOrDefault(fila => fila.CategoriaId == id);
                var deAsistencia = asistencias.FirstOrDefault(fila => fila.CategoriaId == id);
                return new CifrasDeCategoria(
                    categorias[id],
                    dePartidos?.Jugados ?? 0,
                    dePartidos?.Goles ?? 0,
                    deAsistencia?.Registrados ?? 0,
                    deAsistencia?.Asistidos ?? 0);
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<(int Registrados, int Asistidos)> AsistenciaEntreAsync(
        Guid usuarioRolId, DateOnly desde, DateOnly hasta, CancellationToken cancelacion = default)
    {
        var filas = _contexto.AsistenciasEntrenamiento.Where(fila => fila.UsuarioRolId == usuarioRolId
            && fila.Entrenamiento!.Estado != EstadoEntrenamiento.CANCELADO
            && fila.Entrenamiento.Fecha >= desde
            && fila.Entrenamiento.Fecha <= hasta);
        var registrados = await filas.CountAsync(cancelacion);
        var asistidos = registrados == 0 ? 0 : await filas.CountAsync(fila => fila.Presente, cancelacion);
        return (registrados, asistidos);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Categoria>> CategoriasConGolesAsync(CancellationToken cancelacion = default)
    {
        var ids = await ConGoles()
            .Select(fila => fila.CategoriaId)
            .Distinct()
            .ToListAsync(cancelacion);

        return await _contexto.Categorias.AsNoTracking()
            .Where(categoria => ids.Contains(categoria.Id))
            .OrderByDescending(categoria => categoria.Anio)
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public Task<Categoria?> CategoriaAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        _contexto.Categorias.AsNoTracking().FirstOrDefaultAsync(categoria => categoria.Id == categoriaId, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<(UsuarioRol Jugador, int Goles)>> GoleadoresAsync(
        Guid categoriaId, CancellationToken cancelacion = default)
    {
        var goles = await ConGoles()
            .Where(fila => fila.CategoriaId == categoriaId)
            .GroupBy(fila => fila.UsuarioRolId)
            .Select(grupo => new { UsuarioRolId = grupo.Key, Goles = grupo.Sum(fila => fila.Goles) })
            .ToListAsync(cancelacion);

        var ids = goles.Select(fila => fila.UsuarioRolId).ToList();
        var jugadores = await _contexto.UsuariosRol.AsNoTracking()
            .Where(integrante => ids.Contains(integrante.Id))
            .ToDictionaryAsync(integrante => integrante.Id, cancelacion);

        return goles
            .Where(fila => jugadores.ContainsKey(fila.UsuarioRolId))
            .Select(fila => (jugadores[fila.UsuarioRolId], fila.Goles))
            .ToList();
    }

    /// <summary>Las filas con goles de partidos jugados.</summary>
    private IQueryable<PartidoJugador> ConGoles() => _contexto.PartidosJugador
        .Where(fila => fila.Goles > 0 && fila.Partido!.Estado == EstadoPartido.JUGADO);
}
