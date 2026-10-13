using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a la audiencia de los entrenamientos del club de la petición con Entity
/// Framework (spec 013).
/// Su responsabilidad es calcular en una consulta del club los jugadores activos de la audiencia de
/// un entrenamiento (la condición de <c>ReglaAudienciaDeEntrenamiento</c> en SQL), validar equipos y
/// jugadores, buscar jugadores sumables con lo mínimo y reemplazar los equipos y los añadidos.
/// Trabaja siempre con el filtro de aislamiento activo.
/// No decide quién ve el entrenamiento, no abre transacciones y no genera avisos.
/// </summary>
public class RepositorioAudiencia : IRepositorioAudiencia
{
    private readonly ContextoLFClub _contexto;
    private readonly IContextoClub _contextoClub;

    /// <summary>Crea el repositorio sobre el contexto de datos y el club de la petición.</summary>
    public RepositorioAudiencia(ContextoLFClub contexto, IContextoClub contextoClub)
    {
        _contexto = contexto;
        _contextoClub = contextoClub;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipoParaAudiencia>> EquiposAsync(
        IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default)
    {
        var ids = equipoIds.ToList();
        return await _contexto.Equipos.AsNoTracking()
            .Where(equipo => ids.Contains(equipo.Id))
            .Select(equipo => new EquipoParaAudiencia(equipo.Id, equipo.CategoriaId, equipo.Activo, equipo.Categoria!.Activa))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> JugadoresSumablesAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default)
    {
        var ids = usuarioRolIds.ToList();
        var encontrados = await JugadoresActivos().Where(jugador => ids.Contains(jugador.Id))
            .Select(jugador => jugador.Id)
            .ToListAsync(cancelacion);
        return encontrados.ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> AudienciaNaturalAsync(
        IReadOnlyCollection<Guid> categoriaIds, IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default)
    {
        var categorias = categoriaIds.ToList();
        var equipos = equipoIds.ToList();
        var jugadores = equipos.Count > 0
            ? JugadoresActivos().Where(jugador => _contexto.JugadoresEquipo.Any(
                fila => fila.UsuarioRolId == jugador.Id && equipos.Contains(fila.EquipoId)))
            : JugadoresActivos().Where(jugador => categorias.Contains(jugador.CategoriaId!.Value));
        return (await jugadores.Select(jugador => jugador.Id).ToListAsync(cancelacion)).ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> AudienciaDeAsync(Guid entrenamientoId, CancellationToken cancelacion = default)
    {
        var jugadores = JugadoresActivos().Where(jugador =>
            _contexto.EntrenamientosJugador.Any(
                fila => fila.EntrenamientoId == entrenamientoId && fila.UsuarioRolId == jugador.Id)
            || (_contexto.EntrenamientosEquipo.Any(fila => fila.EntrenamientoId == entrenamientoId)
                ? _contexto.JugadoresEquipo.Any(pertenencia => pertenencia.UsuarioRolId == jugador.Id
                    && _contexto.EntrenamientosEquipo.Any(fila => fila.EntrenamientoId == entrenamientoId
                        && fila.EquipoId == pertenencia.EquipoId))
                : _contexto.EntrenamientosCategoria.Any(fila => fila.EntrenamientoId == entrenamientoId
                    && fila.CategoriaId == jugador.CategoriaId)));
        return (await jugadores.Select(jugador => jugador.Id).ToListAsync(cancelacion)).ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JugadorSumableDto>> BuscarSumablesAsync(
        string texto, int limite, CancellationToken cancelacion = default)
    {
        var patron = $"%{Escapar(texto.Trim())}%";
        var filas = await JugadoresActivos()
            .Where(jugador => EF.Functions.ILike(jugador.Nombres, patron, "\\")
                || EF.Functions.ILike(jugador.Apellidos, patron, "\\")
                || EF.Functions.ILike(jugador.Nombres + " " + jugador.Apellidos, patron, "\\"))
            .OrderBy(jugador => jugador.Apellidos).ThenBy(jugador => jugador.Nombres).ThenBy(jugador => jugador.Id)
            .Take(limite)
            .Select(jugador => new { jugador.Id, jugador.Nombres, jugador.Apellidos, Categoria = jugador.CategoriaId!.Value, jugador.Categoria!.Anio })
            .ToListAsync(cancelacion);
        return filas
            .Select(fila => new JugadorSumableDto(
                fila.Id, fila.Nombres, fila.Apellidos, new CategoriaDeActividadDto(fila.Categoria, fila.Anio)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JugadorSumableDto>> AnadidosAsync(
        Guid entrenamientoId, CancellationToken cancelacion = default)
    {
        var filas = await _contexto.EntrenamientosJugador.AsNoTracking()
            .Where(fila => fila.EntrenamientoId == entrenamientoId && fila.Origen == OrigenDeJugadorEnEntrenamiento.ANADIDO)
            .OrderBy(fila => fila.UsuarioRol!.Apellidos).ThenBy(fila => fila.UsuarioRol!.Nombres).ThenBy(fila => fila.UsuarioRolId)
            .Select(fila => new
            {
                fila.UsuarioRolId,
                fila.UsuarioRol!.Nombres,
                fila.UsuarioRol.Apellidos,
                CategoriaId = fila.UsuarioRol.CategoriaId ?? Guid.Empty,
                Anio = fila.UsuarioRol.Categoria != null ? fila.UsuarioRol.Categoria.Anio : 0,
            })
            .ToListAsync(cancelacion);
        return filas
            .Select(fila => new JugadorSumableDto(
                fila.UsuarioRolId, fila.Nombres, fila.Apellidos, new CategoriaDeActividadDto(fila.CategoriaId, fila.Anio)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task ReemplazarAsync(
        Guid entrenamientoId,
        IReadOnlyCollection<Guid> equipoIds,
        IReadOnlyCollection<Guid> anadidoIds,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default)
    {
        var clubId = _contextoClub.ClubId
            ?? throw new InvalidOperationException("No hay club en el contexto de la petición.");
        var equipos = equipoIds.ToList();
        var anadidos = anadidoIds.ToList();

        var equiposActuales = await _contexto.EntrenamientosEquipo
            .Where(fila => fila.EntrenamientoId == entrenamientoId)
            .Select(fila => fila.EquipoId)
            .ToListAsync(cancelacion);
        await _contexto.EntrenamientosEquipo
            .Where(fila => fila.EntrenamientoId == entrenamientoId && !equipos.Contains(fila.EquipoId))
            .ExecuteDeleteAsync(cancelacion);
        var equiposNuevos = equipos.Except(equiposActuales).Select(equipoId => new EntrenamientoEquipo
        {
            EntrenamientoId = entrenamientoId,
            EquipoId = equipoId,
            ClubId = clubId,
        }).ToList();
        _contexto.EntrenamientosEquipo.AddRange(equiposNuevos);

        var jugadoresActuales = await _contexto.EntrenamientosJugador
            .Where(fila => fila.EntrenamientoId == entrenamientoId)
            .Select(fila => fila.UsuarioRolId)
            .ToListAsync(cancelacion);
        await _contexto.EntrenamientosJugador
            .Where(fila => fila.EntrenamientoId == entrenamientoId
                && fila.Origen == OrigenDeJugadorEnEntrenamiento.ANADIDO
                && !anadidos.Contains(fila.UsuarioRolId))
            .ExecuteDeleteAsync(cancelacion);
        var jugadoresNuevos = anadidos.Except(jugadoresActuales).Select(jugadorId => new EntrenamientoJugador
        {
            EntrenamientoId = entrenamientoId,
            UsuarioRolId = jugadorId,
            ClubId = clubId,
            Origen = OrigenDeJugadorEnEntrenamiento.ANADIDO,
            AnadidoEn = ahoraUtc,
        }).ToList();
        _contexto.EntrenamientosJugador.AddRange(jugadoresNuevos);

        await _contexto.SaveChangesAsync(cancelacion);
        equiposNuevos.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
        jugadoresNuevos.ForEach(fila => _contexto.Entry(fila).State = EntityState.Detached);
    }

    /// <inheritdoc />
    public async Task QuitarRespuestasAsync(
        Guid entrenamientoId, IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default)
    {
        if (usuarioRolIds.Count == 0)
        {
            return;
        }

        var ids = usuarioRolIds.ToList();
        await _contexto.RespuestasEntrenamiento
            .Where(fila => fila.EntrenamientoId == entrenamientoId
                && ids.Contains(fila.UsuarioRolId)
                && !_contexto.AsistenciasEntrenamiento.Any(asistencia =>
                    asistencia.EntrenamientoId == entrenamientoId && asistencia.UsuarioRolId == fila.UsuarioRolId))
            .ExecuteDeleteAsync(cancelacion);
    }

    /// <summary>Los jugadores activos del club: aprobados, sin retirar y con categoría.</summary>
    private IQueryable<UsuarioRol> JugadoresActivos() =>
        _contexto.UsuariosRol.AsNoTracking().Where(integrante =>
            integrante.Rol == Rol.JUGADOR
            && integrante.EstadoIngreso == EstadoIngreso.APROBADO
            && integrante.Activo
            && integrante.RetiradoEn == null
            && integrante.CategoriaId != null);

    private static string Escapar(string texto) =>
        texto.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
