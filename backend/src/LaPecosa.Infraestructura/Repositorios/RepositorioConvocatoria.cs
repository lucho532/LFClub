using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los convocados de los partidos del club de la petición con Entity
/// Framework.
/// Su responsabilidad es leer la planilla y los convocables y aplicar los cambios de la
/// convocatoria con sentencias directas. Trabaja siempre con el filtro de aislamiento activo.
/// No decide quién convoca ni a quién, y no se salta el filtro de aislamiento.
/// </summary>
public class RepositorioConvocatoria : IRepositorioConvocatoria
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioConvocatoria(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PartidoJugador>> ConvocadosAsync(Guid partidoId, CancellationToken cancelacion = default) =>
        await _contexto.PartidosJugador.AsNoTracking()
            .Include(fila => fila.UsuarioRol)
            .Include(fila => fila.Categoria)
            .Where(fila => fila.PartidoId == partidoId)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ConvocablesAsync(
        Guid partidoId, IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol.AsNoTracking()
            .Include(integrante => integrante.Categoria)
            .Where(integrante => integrante.Rol == Rol.JUGADOR
                && integrante.EstadoIngreso == EstadoIngreso.APROBADO
                && integrante.Activo
                && integrante.CategoriaId != null
                && categoriaIds.Contains(integrante.CategoriaId.Value)
                && !_contexto.PartidosJugador.Any(fila => fila.PartidoId == partidoId && fila.UsuarioRolId == integrante.Id))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> IntegrantesAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol.AsNoTracking()
            .Where(integrante => usuarioRolIds.Contains(integrante.Id))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task ReemplazarAsync(
        Guid partidoId,
        IReadOnlyCollection<UsuarioRol> altas,
        IReadOnlyCollection<Guid> bajas,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default)
    {
        // Quien sale lo hace sin rastro: su fila y la respuesta de su familia (RF-013).
        if (bajas.Count > 0)
        {
            await _contexto.PartidosJugador
                .Where(fila => fila.PartidoId == partidoId && bajas.Contains(fila.UsuarioRolId))
                .ExecuteDeleteAsync(cancelacion);
        }

        if (altas.Count == 0)
        {
            return;
        }

        var nuevas = altas
            .Select(jugador => new PartidoJugador
            {
                PartidoId = partidoId,
                UsuarioRolId = jugador.Id,
                ClubId = jugador.ClubId,
                CategoriaId = jugador.CategoriaId!.Value,
                ConvocadoEn = ahoraUtc,
            })
            .ToList();
        _contexto.PartidosJugador.AddRange(nuevas);
        await _contexto.SaveChangesAsync(cancelacion);
        nuevas.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
    }
}
