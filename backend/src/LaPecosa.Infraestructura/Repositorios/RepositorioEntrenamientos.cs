using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los entrenamientos del club de la petición con Entity Framework.
/// Su responsabilidad es leerlos con sus categorías, guardarlos y cambiarlos con sentencias
/// directas y, en las listas, aplicar en SQL la condición de visibilidad del modelo de datos
/// ("Consultas de visibilidad"). Trabaja siempre con el filtro de aislamiento activo.
/// No decide quién ve qué: traduce la regla del dominio sin cambiarla. No se salta el filtro de
/// aislamiento ni borra entrenamientos.
/// </summary>
public class RepositorioEntrenamientos : IRepositorioEntrenamientos
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioEntrenamientos(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Entrenamiento entrenamiento, CancellationToken cancelacion = default)
    {
        _contexto.Entrenamientos.Add(entrenamiento);
        await _contexto.SaveChangesAsync(cancelacion);
        entrenamiento.Categorias.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
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
    public async Task<bool?> AsistenciaDeAsync(Guid entrenamientoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.AsistenciasEntrenamiento
            .Where(fila => fila.EntrenamientoId == entrenamientoId && fila.UsuarioRolId == usuarioRolId)
            .Select(fila => (bool?)fila.Presente)
            .FirstOrDefaultAsync(cancelacion);

    /// <summary>
    /// La condición de visibilidad del modelo de datos en SQL: PRESIDENTE o DIRECTIVO; o alguna
    /// categoría que entrena; o la cuenta de un jugador con su categoría actual entre las del
    /// entrenamiento o con asistencia registrada en él.
    /// </summary>
    private IQueryable<Entrenamiento> Visibles(QuienPregunta quien)
    {
        var todos = quien.Rol is Rol.PRESIDENTE or Rol.DIRECTIVO;
        var esJugador = quien.EsJugador;
        var entrena = quien.CategoriasQueEntrena.ToList();
        var suCategoria = quien.CategoriaId;
        var yo = quien.UsuarioRolId;

        return ConCategorias().Where(entrenamiento =>
            todos
            || entrenamiento.Categorias.Any(fila => entrena.Contains(fila.CategoriaId))
            || (esJugador
                && (entrenamiento.Categorias.Any(fila => fila.CategoriaId == suCategoria)
                    || _contexto.AsistenciasEntrenamiento.Any(asistencia =>
                        asistencia.EntrenamientoId == entrenamiento.Id && asistencia.UsuarioRolId == yo))));
    }

    private IQueryable<Entrenamiento> ConCategorias() => _contexto.Entrenamientos
        .AsNoTracking()
        .Include(entrenamiento => entrenamiento.Categorias)
        .ThenInclude(fila => fila.Categoria);
}
