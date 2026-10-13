using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los entrenamientos del club de la petición con Entity Framework.
/// Su responsabilidad es leerlos con sus categorías y sus equipos, guardarlos y cambiarlos con
/// sentencias directas y, en las listas, aplicar en SQL la condición de visibilidad del modelo de
/// datos ("Consultas de visibilidad"; spec 013 la amplía con equipos y añadidos). Trabaja siempre con el filtro de aislamiento activo.
/// No decide quién ve qué: traduce la regla del dominio sin cambiarla. No se salta el filtro de
/// aislamiento ni borra entrenamientos.
/// </summary>
public class RepositorioEntrenamientos : IRepositorioEntrenamientos
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioEntrenamientos(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Entrenamiento entrenamiento, CancellationToken cancelacion = default)
    {
        _contexto.Entrenamientos.Add(entrenamiento);
        await _contexto.SaveChangesAsync(cancelacion);
        entrenamiento.Categorias.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
        entrenamiento.Equipos.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
        entrenamiento.Jugadores.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
        _contexto.Entry(entrenamiento).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public Task<Entrenamiento?> ObtenerAsync(Guid entrenamientoId, CancellationToken cancelacion = default) =>
        ConCategorias().FirstOrDefaultAsync(entrenamiento => entrenamiento.Id == entrenamientoId, cancelacion);

    /// <inheritdoc />
    public Task CambiarDatosAsync(
        Guid entrenamientoId,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        string lugar,
        string? nota,
        CancellationToken cancelacion = default) =>
        _contexto.Entrenamientos
            .Where(entrenamiento => entrenamiento.Id == entrenamientoId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(entrenamiento => entrenamiento.Fecha, fecha)
                    .SetProperty(entrenamiento => entrenamiento.HoraInicio, horaInicio)
                    .SetProperty(entrenamiento => entrenamiento.HoraFin, horaFin)
                    .SetProperty(entrenamiento => entrenamiento.Lugar, lugar)
                    .SetProperty(entrenamiento => entrenamiento.Nota, nota),
                cancelacion);

    /// <inheritdoc />
    public Task CambiarEstadoAsync(Guid entrenamientoId, EstadoEntrenamiento estado, CancellationToken cancelacion = default) =>
        _contexto.Entrenamientos
            .Where(entrenamiento => entrenamiento.Id == entrenamientoId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(entrenamiento => entrenamiento.Estado, estado), cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Entrenamiento>> ListarVisiblesAsync(
        DateOnly desde, DateOnly hasta, QuienPregunta quien, CancellationToken cancelacion = default) =>
        await Visibles(quien)
            .Where(entrenamiento => entrenamiento.Fecha >= desde && entrenamiento.Fecha <= hasta)
            .OrderBy(entrenamiento => entrenamiento.Fecha)
            .ThenBy(entrenamiento => entrenamiento.HoraInicio)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Entrenamiento>> ProximosVisiblesAsync(
        DateOnly hoy, TimeOnly ahora, QuienPregunta quien, int maximo, CancellationToken cancelacion = default) =>
        await Visibles(quien)
            .Where(entrenamiento => entrenamiento.Fecha > hoy
                || (entrenamiento.Fecha == hoy && entrenamiento.HoraFin > ahora))
            .OrderBy(entrenamiento => entrenamiento.Fecha)
            .ThenBy(entrenamiento => entrenamiento.HoraInicio)
            .Take(maximo)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<RespuestaFamilia?> RespuestaDeAsync(
        Guid entrenamientoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.RespuestasEntrenamiento
            .Where(fila => fila.EntrenamientoId == entrenamientoId && fila.UsuarioRolId == usuarioRolId)
            .Select(fila => (RespuestaFamilia?)fila.Respuesta)
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, RespuestaFamilia>> RespuestasDeAsync(
        IReadOnlyCollection<Guid> entrenamientoIds, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.RespuestasEntrenamiento
            .Where(fila => entrenamientoIds.Contains(fila.EntrenamientoId) && fila.UsuarioRolId == usuarioRolId)
            .ToDictionaryAsync(fila => fila.EntrenamientoId, fila => fila.Respuesta, cancelacion);

    /// <inheritdoc />
    public Task<bool> EnListaDeAsync(Guid entrenamientoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.EntrenamientosJugador.AsNoTracking()
            .AnyAsync(fila => fila.EntrenamientoId == entrenamientoId && fila.UsuarioRolId == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public async Task<bool?> AsistenciaDeAsync(Guid entrenamientoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.AsistenciasEntrenamiento
            .Where(fila => fila.EntrenamientoId == entrenamientoId && fila.UsuarioRolId == usuarioRolId)
            .Select(fila => (bool?)fila.Presente)
            .FirstOrDefaultAsync(cancelacion);

    /// <summary>
    /// La condición de visibilidad de <c>ReglaVisibilidadDeEntrenamiento</c> en SQL: el PRESIDENTE;
    /// quien entrena alguna de sus categorías; el DIRECTIVO, además, los que no van dirigidos a
    /// equipos; y la cuenta de un jugador, los que lo tienen en su lista guardada (añadido o
    /// congelado), aquellos con asistencia registrada y los de su audiencia: con equipos, estar en
    /// alguno; sin equipos, su categoría actual.
    /// </summary>
    private IQueryable<Entrenamiento> Visibles(QuienPregunta quien)
    {
        var esPresidente = quien.Rol == Rol.PRESIDENTE;
        var esDirectivo = quien.Rol == Rol.DIRECTIVO;
        var esJugador = quien.EsJugador;
        var entrena = quien.CategoriasQueEntrena.ToList();
        var misEquipos = quien.EquipoIds.ToList();
        var suCategoria = quien.CategoriaId;
        var yo = quien.UsuarioRolId;

        return ConCategorias().Where(entrenamiento =>
            esPresidente
            || entrenamiento.Categorias.Any(fila => entrena.Contains(fila.CategoriaId))
            || (esDirectivo && !entrenamiento.Equipos.Any())
            || (esJugador
                && (entrenamiento.Jugadores.Any(fila => fila.UsuarioRolId == yo)
                    || _contexto.AsistenciasEntrenamiento.Any(asistencia =>
                        asistencia.EntrenamientoId == entrenamiento.Id && asistencia.UsuarioRolId == yo)
                    || (entrenamiento.Equipos.Any()
                        ? entrenamiento.Equipos.Any(fila => misEquipos.Contains(fila.EquipoId))
                        : entrenamiento.Categorias.Any(fila => fila.CategoriaId == suCategoria)))));
    }

    private IQueryable<Entrenamiento> ConCategorias() => _contexto.Entrenamientos
        .AsNoTracking()
        .Include(entrenamiento => entrenamiento.Categorias)
        .ThenInclude(fila => fila.Categoria)
        .Include(entrenamiento => entrenamiento.Equipos)
        .ThenInclude(fila => fila.Equipo)
        .ThenInclude(equipo => equipo!.Categoria);

    /// <inheritdoc />
    public async Task ResponderAsync(
        Guid entrenamientoId, UsuarioRol jugador, RespuestaFamilia respuesta, DateTime ahoraUtc, CancellationToken cancelacion = default)
    {
        var cambiadas = await _contexto.RespuestasEntrenamiento
            .Where(fila => fila.EntrenamientoId == entrenamientoId && fila.UsuarioRolId == jugador.Id)
            .ExecuteUpdateAsync(
                cambios => cambios.SetProperty(fila => fila.Respuesta, respuesta).SetProperty(fila => fila.RespondidaEn, ahoraUtc),
                cancelacion);
        if (cambiadas > 0)
        {
            return;
        }

        var nueva = new RespuestaEntrenamiento
        {
            EntrenamientoId = entrenamientoId,
            UsuarioRolId = jugador.Id,
            ClubId = jugador.ClubId,
            Respuesta = respuesta,
            RespondidaEn = ahoraUtc,
        };
        _contexto.RespuestasEntrenamiento.Add(nueva);
        await _contexto.SaveChangesAsync(cancelacion);
        _contexto.Entry(nueva).State = EntityState.Detached;
    }
}
