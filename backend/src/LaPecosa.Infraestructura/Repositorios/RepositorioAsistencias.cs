using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a la asistencia y a las respuestas de los entrenamientos del club de la
/// petición con Entity Framework.
/// Su responsabilidad es leer la planilla y escribir la asistencia. Trabaja siempre con el filtro de
/// aislamiento activo.
/// No decide quién marca a quién, no cambia la categoría copiada y no se salta el filtro de
/// aislamiento.
/// </summary>
public class RepositorioAsistencias : IRepositorioAsistencias
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioAsistencias(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> JugadoresActivosAsync(
        IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol.AsNoTracking()
            .Include(integrante => integrante.Categoria)
            .Where(integrante => integrante.Rol == Rol.JUGADOR
                && integrante.EstadoIngreso == EstadoIngreso.APROBADO
                && integrante.Activo
                && integrante.CategoriaId != null
                && categoriaIds.Contains(integrante.CategoriaId.Value))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AsistenciaEntrenamiento>> AsistenciasAsync(
        Guid entrenamientoId, CancellationToken cancelacion = default) =>
        await _contexto.AsistenciasEntrenamiento.AsNoTracking()
            .Include(fila => fila.UsuarioRol)
            .Include(fila => fila.Categoria)
            .Where(fila => fila.EntrenamientoId == entrenamientoId)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, RespuestaFamilia>> RespuestasAsync(
        Guid entrenamientoId, CancellationToken cancelacion = default) =>
        await _contexto.RespuestasEntrenamiento
            .Where(fila => fila.EntrenamientoId == entrenamientoId)
            .ToDictionaryAsync(fila => fila.UsuarioRolId, fila => fila.Respuesta, cancelacion);

    /// <inheritdoc />
    public async Task RegistrarAsync(
        Guid entrenamientoId,
        IReadOnlyCollection<(UsuarioRol Jugador, Guid Categoria, bool Presente)> filas,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default)
    {
        var ids = filas.Select(fila => fila.Jugador.Id).ToList();
        var existentes = await _contexto.AsistenciasEntrenamiento
            .Where(fila => fila.EntrenamientoId == entrenamientoId && ids.Contains(fila.UsuarioRolId))
            .ToDictionaryAsync(fila => fila.UsuarioRolId, cancelacion);

        foreach (var (jugador, categoria, presente) in filas)
        {
            if (existentes.TryGetValue(jugador.Id, out var existente))
            {
                // Corregir no cambia la categoría copiada la primera vez (§13).
                existente.Presente = presente;
                existente.RegistradaEn = ahoraUtc;
                continue;
            }

            _contexto.AsistenciasEntrenamiento.Add(new AsistenciaEntrenamiento
            {
                EntrenamientoId = entrenamientoId,
                UsuarioRolId = jugador.Id,
                ClubId = jugador.ClubId,
                CategoriaId = categoria,
                Presente = presente,
                RegistradaEn = ahoraUtc,
            });
        }

        await _contexto.SaveChangesAsync(cancelacion);
        _contexto.ChangeTracker.Clear();
    }
}
