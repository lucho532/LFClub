using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a las asignaciones de entrenadores del club de la petición con Entity
/// Framework.
/// Su responsabilidad es leerlas y cambiarlas con sentencias directas. Trabaja siempre con el
/// filtro de aislamiento activo: sin club en el contexto no devuelve ni cambia nada.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioAsignaciones : IRepositorioAsignaciones
{
    private readonly ContextoLFClub _contexto;
    private readonly IContextoClub _contextoClub;

    /// <summary>Crea el repositorio sobre el contexto de datos y el club de la petición.</summary>
    public RepositorioAsignaciones(ContextoLFClub contexto, IContextoClub contextoClub)
    {
        _contexto = contexto;
        _contextoClub = contextoClub;
    }

    private Guid ClubDeLaPeticion => _contextoClub.ClubId
        ?? throw new InvalidOperationException("No hay club en el contexto de la petición.");

    /// <inheritdoc />
    public Task<AsignacionEntrenadorCategoria?> ObtenerAsync(
        Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        De(categoriaId, usuarioRolId).AsNoTracking().FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public Task<bool> TieneActivaAsync(Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        De(categoriaId, usuarioRolId).AnyAsync(asignacion => asignacion.Activa, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> CategoriasDeAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.AsignacionesEntrenadorCategoria
            .Where(asignacion => asignacion.UsuarioRolId == usuarioRolId && asignacion.Activa)
            .Select(asignacion => asignacion.CategoriaId)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarIntegrantesAprobadosAsync(CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol.AsNoTracking()
            .Where(integrante => integrante.EstadoIngreso == EstadoIngreso.APROBADO && integrante.Activo)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task AsignarAsync(
        Guid categoriaId, Guid usuarioRolId, DateTime ahoraUtc, CancellationToken cancelacion = default)
    {
        // Reasignar reutiliza la fila de esa pareja: nunca hay dos (research §7).
        var existentes = await De(categoriaId, usuarioRolId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(asignacion => asignacion.Activa, true), cancelacion);
        if (existentes > 0)
        {
            return;
        }

        var nueva = new AsignacionEntrenadorCategoria
        {
            ClubId = ClubDeLaPeticion,
            CategoriaId = categoriaId,
            UsuarioRolId = usuarioRolId,
            Activa = true,
            CreadaEn = ahoraUtc,
        };

        _contexto.AsignacionesEntrenadorCategoria.Add(nueva);
        await _contexto.SaveChangesAsync(cancelacion);
        _contexto.Entry(nueva).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task RetirarAsync(Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        await _contexto.EntrenadoresEquipo
            .Where(fila => fila.Asignacion!.CategoriaId == categoriaId && fila.Asignacion.UsuarioRolId == usuarioRolId)
            .ExecuteDeleteAsync(cancelacion);

        await De(categoriaId, usuarioRolId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(asignacion => asignacion.Activa, false), cancelacion);
    }

    /// <inheritdoc />
    public async Task ReemplazarEquiposAsync(
        Guid asignacionId, IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default)
    {
        var deLaAsignacion = _contexto.EntrenadoresEquipo
            .Where(fila => fila.AsignacionEntrenadorCategoriaId == asignacionId);

        await deLaAsignacion.Where(fila => !equipoIds.Contains(fila.EquipoId)).ExecuteDeleteAsync(cancelacion);
        var actuales = await deLaAsignacion.Select(fila => fila.EquipoId).ToListAsync(cancelacion);

        var nuevas = equipoIds.Except(actuales)
            .Select(equipoId => new EntrenadorEquipo
            {
                AsignacionEntrenadorCategoriaId = asignacionId, EquipoId = equipoId, ClubId = ClubDeLaPeticion,
            })
            .ToList();
        if (nuevas.Count == 0)
        {
            return;
        }

        _contexto.EntrenadoresEquipo.AddRange(nuevas);
        await _contexto.SaveChangesAsync(cancelacion);
        nuevas.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
    }

    private IQueryable<AsignacionEntrenadorCategoria> De(Guid categoriaId, Guid usuarioRolId) =>
        _contexto.AsignacionesEntrenadorCategoria
            .Where(asignacion => asignacion.CategoriaId == categoriaId && asignacion.UsuarioRolId == usuarioRolId);
}
