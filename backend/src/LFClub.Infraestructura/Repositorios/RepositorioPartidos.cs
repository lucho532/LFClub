using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los partidos del club de la petición con Entity Framework.
/// Su responsabilidad es leerlos con sus categorías, guardarlos y cambiarlos con sentencias
/// directas y, en las listas y el calendario, aplicar en SQL la condición de visibilidad del
/// modelo de datos ("Consultas de visibilidad"). Trabaja siempre con el filtro de aislamiento activo.
/// No decide quién ve qué: traduce la regla del dominio sin cambiarla. No toca la convocatoria, no
/// se salta el filtro de aislamiento y no borra partidos.
/// </summary>
public class RepositorioPartidos : IRepositorioPartidos
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioPartidos(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Partido partido, CancellationToken cancelacion = default)
    {
        _contexto.Partidos.Add(partido);
        await _contexto.SaveChangesAsync(cancelacion);
        partido.Categorias.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
        _contexto.Entry(partido).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public Task<Partido?> ObtenerAsync(Guid partidoId, CancellationToken cancelacion = default) =>
        ConCategorias().FirstOrDefaultAsync(partido => partido.Id == partidoId, cancelacion);

    /// <inheritdoc />
    public Task<PartidoJugador?> FilaDeAsync(Guid partidoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.PartidosJugador.AsNoTracking()
            .FirstOrDefaultAsync(fila => fila.PartidoId == partidoId && fila.UsuarioRolId == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public Task CambiarDatosAsync(Guid partidoId, Partido datos, CancellationToken cancelacion = default) =>
        _contexto.Partidos
            .Where(partido => partido.Id == partidoId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(partido => partido.Rival, datos.Rival)
                    .SetProperty(partido => partido.Fecha, datos.Fecha)
                    .SetProperty(partido => partido.Hora, datos.Hora)
                    .SetProperty(partido => partido.Lugar, datos.Lugar)
                    .SetProperty(partido => partido.Condicion, datos.Condicion)
                    .SetProperty(partido => partido.Tipo, datos.Tipo)
                    .SetProperty(partido => partido.NombreTorneo, datos.NombreTorneo),
                cancelacion);

    /// <inheritdoc />
    public Task CambiarRivalAsync(Guid partidoId, string? rival, CancellationToken cancelacion = default) =>
        _contexto.Partidos
            .Where(partido => partido.Id == partidoId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(partido => partido.Rival, rival), cancelacion);

    /// <inheritdoc />
    public Task CambiarEstadoAsync(Guid partidoId, EstadoPartido estado, CancellationToken cancelacion = default) =>
        _contexto.Partidos
            .Where(partido => partido.Id == partidoId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(partido => partido.Estado, estado), cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Partido>> ListarVisiblesAsync(
        EstadoPartido? estado, Guid? categoriaId, QuienPregunta quien, CancellationToken cancelacion = default)
    {
        var consulta = Visibles(quien);
        if (estado is not null)
        {
            consulta = consulta.Where(partido => partido.Estado == estado);
        }

        if (categoriaId is not null)
        {
            consulta = consulta.Where(partido => partido.Categorias.Any(fila => fila.CategoriaId == categoriaId));
        }

        return await consulta.OrderBy(partido => partido.Fecha).ThenBy(partido => partido.Hora).ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Partido>> EnRangoVisiblesAsync(
        DateOnly desde, DateOnly hasta, QuienPregunta quien, CancellationToken cancelacion = default) =>
        await Visibles(quien)
            .Where(partido => partido.Fecha >= desde && partido.Fecha <= hasta)
            .OrderBy(partido => partido.Fecha)
            .ThenBy(partido => partido.Hora)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, RespuestaFamilia>> RespuestasDeAsync(
        IReadOnlyCollection<Guid> partidoIds, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.PartidosJugador
            .Where(fila => partidoIds.Contains(fila.PartidoId) && fila.UsuarioRolId == usuarioRolId && fila.Respuesta != null)
            .ToDictionaryAsync(fila => fila.PartidoId, fila => fila.Respuesta!.Value, cancelacion);

    /// <summary>
    /// La condición de visibilidad del modelo de datos en SQL: jugado; o PRESIDENTE; o alguna
    /// categoría que entrena; o la cuenta de un jugador convocado.
    /// </summary>
    private IQueryable<Partido> Visibles(QuienPregunta quien)
    {
        var esPresidente = quien.Rol == Rol.PRESIDENTE;
        var esJugador = quien.EsJugador;
        var entrena = quien.CategoriasQueEntrena.ToList();
        var yo = quien.UsuarioRolId;

        return ConCategorias().Where(partido =>
            partido.Estado == EstadoPartido.JUGADO
            || esPresidente
            || partido.Categorias.Any(fila => entrena.Contains(fila.CategoriaId))
            || (esJugador && partido.Jugadores.Any(fila => fila.UsuarioRolId == yo)));
    }

    private IQueryable<Partido> ConCategorias() => _contexto.Partidos
        .AsNoTracking()
        .Include(partido => partido.Categorias)
        .ThenInclude(fila => fila.Categoria);
}
