using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los convocados de los partidos del club de la petición con Entity
/// Framework.
/// Su responsabilidad es leer la planilla y los convocables y aplicar los cambios de la
/// convocatoria y del resultado con sentencias directas: el de quien gestiona, fila por fila con su
/// origen, y lo que publica o indica la familia de un convocado (spec 010). Trabaja siempre con el
/// filtro de aislamiento activo.
/// No decide quién convoca ni a quién, y no se salta el filtro de aislamiento.
/// </summary>
public class RepositorioConvocatoria : IRepositorioConvocatoria
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioConvocatoria(ContextoLFClub contexto)
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

    /// <inheritdoc />
    public async Task GuardarResultadoAsync(
        Guid partidoId,
        int golesClub,
        int golesRival,
        IReadOnlyCollection<FilaGuardada> filas,
        DateTime ahoraUtc,
        Guid autorUsuarioId,
        string autorNombre,
        CancellationToken cancelacion = default)
    {
        // La primera publicación no cambia; el origen pasa a GESTION y ya no vuelve (spec 010).
        await _contexto.Partidos
            .Where(partido => partido.Id == partidoId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(partido => partido.Estado, EstadoPartido.JUGADO)
                    .SetProperty(partido => partido.GolesClub, golesClub)
                    .SetProperty(partido => partido.GolesRival, golesRival)
                    .SetProperty(partido => partido.ResultadoCambiadoEn, ahoraUtc)
                    .SetProperty(partido => partido.ResultadoCambiadoPorUsuarioId, autorUsuarioId)
                    .SetProperty(partido => partido.ResultadoCambiadoPorNombre, autorNombre)
                    .SetProperty(partido => partido.OrigenResultado, OrigenResultado.GESTION)
                    .SetProperty(partido => partido.PublicadoEn, partido => partido.PublicadoEn ?? ahoraUtc)
                    .SetProperty(
                        partido => partido.PublicadoPorUsuarioId,
                        partido => partido.PublicadoEn == null ? autorUsuarioId : partido.PublicadoPorUsuarioId)
                    .SetProperty(partido => partido.PublicadoPorNombre, partido => partido.PublicadoPorNombre ?? autorNombre),
                cancelacion);

        // Cada convocado queda como se envió; la fila corregida pasa a GESTION con quien la corrigió.
        foreach (var guardada in filas)
        {
            var consulta = _contexto.PartidosJugador
                .Where(fila => fila.PartidoId == partidoId && fila.UsuarioRolId == guardada.UsuarioRolId);
            if (guardada.Corregida)
            {
                await consulta.ExecuteUpdateAsync(
                    cambios => cambios
                        .SetProperty(fila => fila.Jugo, guardada.Jugo)
                        .SetProperty(fila => fila.Goles, guardada.Goles)
                        .SetProperty(fila => fila.IndicadoPor, OrigenResultado.GESTION)
                        .SetProperty(fila => fila.IndicadoEn, ahoraUtc)
                        .SetProperty(fila => fila.IndicadoPorUsuarioId, autorUsuarioId)
                        .SetProperty(fila => fila.IndicadoPorNombre, autorNombre),
                    cancelacion);
            }
            else
            {
                await consulta.ExecuteUpdateAsync(
                    cambios => cambios.SetProperty(fila => fila.Jugo, guardada.Jugo).SetProperty(fila => fila.Goles, guardada.Goles),
                    cancelacion);
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> GolesDeLosDemasAsync(Guid partidoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.PartidosJugador
            .Where(fila => fila.PartidoId == partidoId && fila.UsuarioRolId != usuarioRolId)
            .SumAsync(fila => fila.Goles, cancelacion);

    /// <inheritdoc />
    public Task PublicarPorFamiliaAsync(
        Guid partidoId,
        int golesClub,
        int golesRival,
        DateTime ahoraUtc,
        Guid autorUsuarioId,
        string autorNombre,
        CancellationToken cancelacion = default) =>
        _contexto.Partidos
            .Where(partido => partido.Id == partidoId && partido.GolesClub == null)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(partido => partido.Estado, EstadoPartido.JUGADO)
                    .SetProperty(partido => partido.GolesClub, golesClub)
                    .SetProperty(partido => partido.GolesRival, golesRival)
                    .SetProperty(partido => partido.ResultadoCambiadoEn, ahoraUtc)
                    .SetProperty(partido => partido.ResultadoCambiadoPorUsuarioId, autorUsuarioId)
                    .SetProperty(partido => partido.ResultadoCambiadoPorNombre, autorNombre)
                    .SetProperty(partido => partido.OrigenResultado, OrigenResultado.FAMILIA)
                    .SetProperty(partido => partido.PublicadoEn, ahoraUtc)
                    .SetProperty(partido => partido.PublicadoPorUsuarioId, autorUsuarioId)
                    .SetProperty(partido => partido.PublicadoPorNombre, autorNombre)
                    .SetProperty(partido => partido.PublicadoPorFamilia, true),
                cancelacion);

    /// <inheritdoc />
    public Task IndicarPorFamiliaAsync(
        Guid partidoId,
        Guid usuarioRolId,
        bool jugo,
        int goles,
        DateTime ahoraUtc,
        Guid autorUsuarioId,
        string autorNombre,
        CancellationToken cancelacion = default) =>
        _contexto.PartidosJugador
            .Where(fila => fila.PartidoId == partidoId && fila.UsuarioRolId == usuarioRolId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(fila => fila.Jugo, jugo)
                    .SetProperty(fila => fila.Goles, goles)
                    .SetProperty(fila => fila.IndicadoPor, OrigenResultado.FAMILIA)
                    .SetProperty(fila => fila.IndicadoEn, ahoraUtc)
                    .SetProperty(fila => fila.IndicadoPorUsuarioId, autorUsuarioId)
                    .SetProperty(fila => fila.IndicadoPorNombre, autorNombre),
                cancelacion);

    /// <inheritdoc />
    public Task ResponderAsync(
        Guid partidoId, Guid usuarioRolId, RespuestaFamilia respuesta, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        _contexto.PartidosJugador
            .Where(fila => fila.PartidoId == partidoId && fila.UsuarioRolId == usuarioRolId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(fila => fila.Respuesta, respuesta)
                    .SetProperty(fila => fila.RespondidaEn, ahoraUtc),
                cancelacion);
}
