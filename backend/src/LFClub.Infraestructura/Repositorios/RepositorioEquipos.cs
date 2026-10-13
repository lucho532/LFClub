using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los equipos del club de la petición con Entity Framework.
/// Su responsabilidad es leer los equipos activos y cambiarlos con sentencias directas y, al
/// desactivar uno, congelar su audiencia en los entrenamientos próximos (spec 013). Trabaja
/// siempre con el filtro de aislamiento activo: sin club en el contexto no devuelve ni cambia nada.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioEquipos : IRepositorioEquipos
{
    private readonly ContextoLFClub _contexto;
    private readonly IContextoClub _contextoClub;

    /// <summary>Crea el repositorio sobre el contexto de datos y el club de la petición.</summary>
    public RepositorioEquipos(ContextoLFClub contexto, IContextoClub contextoClub)
    {
        _contexto = contexto;
        _contextoClub = contextoClub;
    }

    private IQueryable<Equipo> Activos => _contexto.Equipos.Where(equipo => equipo.Activo);

    /// <inheritdoc />
    public Task<Equipo?> ObtenerActivoAsync(Guid categoriaId, Guid equipoId, CancellationToken cancelacion = default) =>
        Activos.AsNoTracking()
            .FirstOrDefaultAsync(equipo => equipo.Id == equipoId && equipo.CategoriaId == categoriaId, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> EquiposDeJugadorAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.JugadoresEquipo
            .Where(fila => fila.UsuarioRolId == usuarioRolId && fila.Equipo!.Activo)
            .Select(fila => fila.EquipoId)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListarActivosAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        await Activos.Where(equipo => equipo.CategoriaId == categoriaId).Select(equipo => equipo.Id).ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(
        Guid categoriaId, string nombreNormalizado, Guid? exceptoEquipoId, CancellationToken cancelacion = default) =>
        Activos.AnyAsync(
            equipo => equipo.CategoriaId == categoriaId
                && equipo.NombreNormalizado == nombreNormalizado
                && equipo.Id != exceptoEquipoId,
            cancelacion);

    /// <inheritdoc />
    public async Task AgregarAsync(Equipo equipo, CancellationToken cancelacion = default)
    {
        _contexto.Equipos.Add(equipo);
        await _contexto.SaveChangesAsync(cancelacion);
        _contexto.Entry(equipo).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public Task RenombrarAsync(
        Guid equipoId, string nombre, string nombreNormalizado, CancellationToken cancelacion = default) =>
        Activos
            .Where(equipo => equipo.Id == equipoId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(equipo => equipo.Nombre, nombre)
                    .SetProperty(equipo => equipo.NombreNormalizado, nombreNormalizado),
                cancelacion);

    /// <inheritdoc />
    public async Task DesactivarAsync(
        Guid equipoId, DateOnly desde, DateTime ahoraUtc, CancellationToken cancelacion = default)
    {
        await CongelarAudienciaAsync(equipoId, desde, ahoraUtc, cancelacion);
        await _contexto.JugadoresEquipo.Where(fila => fila.EquipoId == equipoId).ExecuteDeleteAsync(cancelacion);
        await _contexto.EntrenadoresEquipo.Where(fila => fila.EquipoId == equipoId).ExecuteDeleteAsync(cancelacion);
        await _contexto.Equipos
            .Where(equipo => equipo.Id == equipoId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(equipo => equipo.Activo, false), cancelacion);
    }

    /// <summary>
    /// Copia a cada jugador activo del equipo a los entrenamientos con fecha desde <paramref name="desde"/>
    /// dirigidos a él, cancelados incluidos, con origen <c>EQUIPO_DESACTIVADO</c> (RF-026). Quien ya
    /// tiene fila en uno (añadido) no se toca.
    /// </summary>
    private async Task CongelarAudienciaAsync(
        Guid equipoId, DateOnly desde, DateTime ahoraUtc, CancellationToken cancelacion)
    {
        var entrenamientos = await _contexto.EntrenamientosEquipo
            .Where(fila => fila.EquipoId == equipoId && fila.Entrenamiento!.Fecha >= desde)
            .Select(fila => fila.EntrenamientoId)
            .ToListAsync(cancelacion);
        if (entrenamientos.Count == 0)
        {
            return;
        }

        var jugadores = await _contexto.JugadoresEquipo
            .Where(fila => fila.EquipoId == equipoId
                && fila.UsuarioRol!.EstadoIngreso == EstadoIngreso.APROBADO
                && fila.UsuarioRol.Activo
                && fila.UsuarioRol.RetiradoEn == null)
            .Select(fila => fila.UsuarioRolId)
            .ToListAsync(cancelacion);
        if (jugadores.Count == 0)
        {
            return;
        }

        var existentes = (await _contexto.EntrenamientosJugador
            .Where(fila => entrenamientos.Contains(fila.EntrenamientoId) && jugadores.Contains(fila.UsuarioRolId))
            .Select(fila => new { fila.EntrenamientoId, fila.UsuarioRolId })
            .ToListAsync(cancelacion))
            .Select(fila => (fila.EntrenamientoId, fila.UsuarioRolId))
            .ToHashSet();
        var clubId = _contextoClub.ClubId
            ?? throw new InvalidOperationException("No hay club en el contexto de la petición.");

        var nuevas = (from entrenamientoId in entrenamientos
                      from jugadorId in jugadores
                      where !existentes.Contains((entrenamientoId, jugadorId))
                      select new EntrenamientoJugador
                      {
                          EntrenamientoId = entrenamientoId,
                          UsuarioRolId = jugadorId,
                          ClubId = clubId,
                          Origen = OrigenDeJugadorEnEntrenamiento.EQUIPO_DESACTIVADO,
                          EquipoDeOrigenId = equipoId,
                          AnadidoEn = ahoraUtc,
                      }).ToList();
        _contexto.EntrenamientosJugador.AddRange(nuevas);
        await _contexto.SaveChangesAsync(cancelacion);
        foreach (var fila in nuevas)
        {
            _contexto.Entry(fila).State = EntityState.Detached;
        }
    }

    /// <inheritdoc />
    public async Task<bool> BorrarSiNuncaSeUsoAsync(Guid equipoId, CancellationToken cancelacion = default) =>
        await Activos.Where(equipo => equipo.Id == equipoId && !equipo.Usado).ExecuteDeleteAsync(cancelacion) == 1;

    /// <inheritdoc />
    public Task MarcarUsadosAsync(IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default) =>
        equipoIds.Count == 0
            ? Task.CompletedTask
            : _contexto.Equipos
                .Where(equipo => equipoIds.Contains(equipo.Id) && !equipo.Usado)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(equipo => equipo.Usado, true), cancelacion);

    /// <inheritdoc />
    public async Task PonerJugadorAsync(Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var yaEsta = await _contexto.JugadoresEquipo
            .AnyAsync(fila => fila.EquipoId == equipoId && fila.UsuarioRolId == usuarioRolId, cancelacion);
        if (yaEsta)
        {
            return;
        }

        var fila = new JugadorEquipo
        {
            EquipoId = equipoId,
            UsuarioRolId = usuarioRolId,
            ClubId = _contextoClub.ClubId
                ?? throw new InvalidOperationException("No hay club en el contexto de la petición."),
        };

        _contexto.JugadoresEquipo.Add(fila);
        await _contexto.SaveChangesAsync(cancelacion);
        _contexto.Entry(fila).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public Task SacarJugadorAsync(Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.JugadoresEquipo
            .Where(fila => fila.EquipoId == equipoId && fila.UsuarioRolId == usuarioRolId)
            .ExecuteDeleteAsync(cancelacion);
}
