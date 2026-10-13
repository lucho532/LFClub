using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa la parte de <see cref="RepositorioAvisos"/> que ve la cuenta de un jugador (spec 012,
/// "Avisos de una cuenta" del modelo de datos).
/// Su responsabilidad es traducir a SQL qué filas cuentan para la cuenta (jugador activo y, si la
/// sesión está limitada, solo esos jugadores), agrupar por aviso para que uno que llega por dos
/// hermanos salga una vez, ordenar por identificador descendente y paginar con cursor, contar los
/// sin leer, poner la lectura de la cuenta y calcular la disponibilidad de la actividad.
/// No decide quién es la cuenta ni qué rol puede pedirlo, y no borra nada.
/// </summary>
public partial class RepositorioAvisos
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AvisoDeCuenta>> ListarVisiblesAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid>? limitacion,
        Guid? antesDe,
        int limite,
        CancellationToken cancelacion = default)
    {
        var grupos = Agrupados(usuarioId, limitacion);
        if (antesDe is { } cursor)
        {
            grupos = grupos.Where(grupo => grupo.AvisoId.CompareTo(cursor) < 0);
        }

        var pagina = await grupos.OrderByDescending(grupo => grupo.AvisoId).Take(limite).ToListAsync(cancelacion);
        return await ConsultarAsync(pagina, cancelacion);
    }

    /// <inheritdoc />
    public Task<int> ContarSinLeerAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default) =>
        Visibles(usuarioId, limitacion)
            .Where(fila => fila.LeidoEn == null)
            .Select(fila => fila.AvisoId)
            .Distinct()
            .CountAsync(cancelacion);

    /// <inheritdoc />
    public async Task<AvisoDeCuenta?> ObtenerParaLecturaAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, Guid avisoId, CancellationToken cancelacion = default)
    {
        var grupo = await Agrupados(usuarioId, limitacion)
            .Where(fila => fila.AvisoId == avisoId)
            .FirstOrDefaultAsync(cancelacion);
        return grupo is null ? null : (await ConsultarAsync([grupo], cancelacion)).SingleOrDefault();
    }

    /// <inheritdoc />
    public Task MarcarLeidoAsync(Guid usuarioId, Guid avisoId, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        _contexto.AvisosDeActividadDestinatario
            .Where(fila => fila.UsuarioId == usuarioId && fila.AvisoId == avisoId && fila.LeidoEn == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.LeidoEn, ahoraUtc), cancelacion);

    /// <inheritdoc />
    public async Task MarcarTodosLeidosAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, DateTime ahoraUtc, CancellationToken cancelacion = default)
    {
        var pendientes = await Visibles(usuarioId, limitacion)
            .Where(fila => fila.LeidoEn == null)
            .Select(fila => fila.AvisoId)
            .Distinct()
            .ToListAsync(cancelacion);
        if (pendientes.Count == 0)
        {
            return;
        }

        await _contexto.AvisosDeActividadDestinatario
            .Where(fila => fila.UsuarioId == usuarioId && pendientes.Contains(fila.AvisoId) && fila.LeidoEn == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.LeidoEn, ahoraUtc), cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> DisponibilidadAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid>? limitacion,
        IReadOnlyCollection<Guid> entrenamientoIds,
        IReadOnlyCollection<Guid> partidoIds,
        CancellationToken cancelacion = default)
    {
        var disponibles = new HashSet<Guid>();
        var jugadores = JugadoresActivos(usuarioId, limitacion);

        if (entrenamientoIds.Count > 0)
        {
            var ids = entrenamientoIds.ToList();

            // La audiencia de la spec 013: con equipos, estar en alguno; sin equipos, la categoría; y
            // siempre, estar en la lista guardada del entrenamiento.
            var porAudiencia = await _contexto.Entrenamientos.AsNoTracking()
                .Where(entrenamiento => ids.Contains(entrenamiento.Id)
                    && jugadores.Any(jugador =>
                        entrenamiento.Jugadores.Any(fila => fila.UsuarioRolId == jugador.Id)
                        || (entrenamiento.Equipos.Any()
                            ? entrenamiento.Equipos.Any(fila => _contexto.JugadoresEquipo.Any(
                                pertenencia => pertenencia.UsuarioRolId == jugador.Id && pertenencia.EquipoId == fila.EquipoId))
                            : entrenamiento.Categorias.Any(fila => fila.CategoriaId == jugador.CategoriaId))))
                .Select(entrenamiento => entrenamiento.Id)
                .ToListAsync(cancelacion);
            var porAsistencia = await _contexto.AsistenciasEntrenamiento.AsNoTracking()
                .Where(fila => ids.Contains(fila.EntrenamientoId)
                    && jugadores.Any(jugador => jugador.Id == fila.UsuarioRolId))
                .Select(fila => fila.EntrenamientoId)
                .ToListAsync(cancelacion);
            disponibles.UnionWith(porAudiencia);
            disponibles.UnionWith(porAsistencia);
        }

        if (partidoIds.Count > 0)
        {
            var ids = partidoIds.ToList();
            var jugados = await _contexto.Partidos.AsNoTracking()
                .Where(partido => ids.Contains(partido.Id) && partido.Estado == EstadoPartido.JUGADO)
                .Select(partido => partido.Id)
                .ToListAsync(cancelacion);
            var convocados = await _contexto.PartidosJugador.AsNoTracking()
                .Where(fila => ids.Contains(fila.PartidoId)
                    && jugadores.Any(jugador => jugador.Id == fila.UsuarioRolId))
                .Select(fila => fila.PartidoId)
                .ToListAsync(cancelacion);
            disponibles.UnionWith(jugados);
            disponibles.UnionWith(convocados);
        }

        return disponibles;
    }

    /// <summary>Los jugadores activos de la cuenta: aprobados, sin retirar y dentro de la limitación.</summary>
    private IQueryable<UsuarioRol> JugadoresActivos(Guid usuarioId, IReadOnlyCollection<Guid>? limitacion)
    {
        var jugadores = _contexto.UsuariosRol.AsNoTracking().Where(integrante =>
            integrante.UsuarioId == usuarioId
            && integrante.Rol == Rol.JUGADOR
            && integrante.EstadoIngreso == EstadoIngreso.APROBADO
            && integrante.Activo);

        if (limitacion is null)
        {
            return jugadores;
        }

        var permitidos = limitacion.ToList();
        return jugadores.Where(integrante => permitidos.Contains(integrante.Id));
    }

    /// <summary>
    /// Las filas que cuentan para la cuenta: de su cuenta, de un jugador activo y, si la sesión está
    /// limitada, de esos jugadores.
    /// </summary>
    private IQueryable<AvisoDeActividadDestinatario> Visibles(Guid usuarioId, IReadOnlyCollection<Guid>? limitacion)
    {
        var filas = _contexto.AvisosDeActividadDestinatario.AsNoTracking().Where(fila =>
            fila.UsuarioId == usuarioId
            && fila.UsuarioRol!.Rol == Rol.JUGADOR
            && fila.UsuarioRol.EstadoIngreso == EstadoIngreso.APROBADO
            && fila.UsuarioRol.Activo);

        if (limitacion is null)
        {
            return filas;
        }

        var jugadores = limitacion.ToList();
        return filas.Where(fila => jugadores.Contains(fila.UsuarioRolId));
    }

    /// <summary>Una fila por aviso: sin leer si alguna fila de la cuenta lo está.</summary>
    private IQueryable<GrupoDeAviso> Agrupados(Guid usuarioId, IReadOnlyCollection<Guid>? limitacion) =>
        Visibles(usuarioId, limitacion)
            .GroupBy(fila => fila.AvisoId)
            .Select(grupo => new GrupoDeAviso
            {
                AvisoId = grupo.Key,
                SinLeer = grupo.Any(fila => fila.LeidoEn == null),
                LeidoEn = grupo.Max(fila => fila.LeidoEn),
            });

    private async Task<IReadOnlyList<AvisoDeCuenta>> ConsultarAsync(
        IReadOnlyList<GrupoDeAviso> grupos, CancellationToken cancelacion)
    {
        var ids = grupos.Select(grupo => grupo.AvisoId).ToList();
        var avisos = await _contexto.AvisosDeActividad.AsNoTracking()
            .Where(aviso => ids.Contains(aviso.Id))
            .ToDictionaryAsync(aviso => aviso.Id, cancelacion);

        return grupos
            .Where(grupo => avisos.ContainsKey(grupo.AvisoId))
            .Select(grupo => new AvisoDeCuenta(
                avisos[grupo.AvisoId], !grupo.SinLeer, grupo.SinLeer ? null : grupo.LeidoEn))
            .ToList();
    }

    /// <summary>El resultado de agrupar las filas de una cuenta por aviso.</summary>
    private sealed class GrupoDeAviso
    {
        public Guid AvisoId { get; init; }

        public bool SinLeer { get; init; }

        public DateTime? LeidoEn { get; init; }
    }
}
