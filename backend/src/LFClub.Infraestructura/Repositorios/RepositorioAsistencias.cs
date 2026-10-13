using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

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
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioAsistencias(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> JugadoresDeLaAudienciaAsync(
        Guid entrenamientoId,
        IReadOnlyCollection<Guid> categoriaIds,
        bool conAnadidos,
        CancellationToken cancelacion = default)
    {
        var categorias = categoriaIds.ToList();
        var tieneEquipos = await _contexto.EntrenamientosEquipo
            .AnyAsync(fila => fila.EntrenamientoId == entrenamientoId, cancelacion);

        return await _contexto.UsuariosRol.AsNoTracking()
            .Include(integrante => integrante.Categoria)
            .Where(integrante => integrante.Rol == Rol.JUGADOR
                && integrante.EstadoIngreso == EstadoIngreso.APROBADO
                && integrante.Activo
                && integrante.CategoriaId != null
                && ((categorias.Contains(integrante.CategoriaId.Value)
                        && (!tieneEquipos
                            || _contexto.JugadoresEquipo.Any(pertenencia => pertenencia.UsuarioRolId == integrante.Id
                                && _contexto.EntrenamientosEquipo.Any(fila => fila.EntrenamientoId == entrenamientoId
                                    && fila.EquipoId == pertenencia.EquipoId))
                            || _contexto.EntrenamientosJugador.Any(fila => fila.EntrenamientoId == entrenamientoId
                                && fila.UsuarioRolId == integrante.Id
                                && fila.Origen == OrigenDeJugadorEnEntrenamiento.EQUIPO_DESACTIVADO)))
                    || (conAnadidos
                        && _contexto.EntrenamientosJugador.Any(fila => fila.EntrenamientoId == entrenamientoId
                            && fila.UsuarioRolId == integrante.Id
                            && fila.Origen == OrigenDeJugadorEnEntrenamiento.ANADIDO))))
            .ToListAsync(cancelacion);
    }

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
    public async Task<IReadOnlyCollection<Guid>> RegistrarAsync(
        Guid entrenamientoId,
        IReadOnlyCollection<(UsuarioRol Jugador, Guid Categoria, bool Presente)> filas,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default)
    {
        var ids = filas.Select(fila => fila.Jugador.Id).ToList();
        var existentes = await _contexto.AsistenciasEntrenamiento
            .Where(fila => fila.EntrenamientoId == entrenamientoId && ids.Contains(fila.UsuarioRolId))
            .ToDictionaryAsync(fila => fila.UsuarioRolId, cancelacion);

        var conCambio = new HashSet<Guid>();
        foreach (var (jugador, categoria, presente) in filas)
        {
            if (existentes.TryGetValue(jugador.Id, out var existente))
            {
                // Corregir no cambia la categoría copiada la primera vez (§13).
                if (existente.Presente != presente)
                {
                    conCambio.Add(existente.CategoriaId);
                }

                existente.Presente = presente;
                existente.RegistradaEn = ahoraUtc;
                continue;
            }

            conCambio.Add(categoria);

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
        return conCambio;
    }
}
