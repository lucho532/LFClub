using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los equipos del club de la petición con Entity Framework.
/// Su responsabilidad es leer los equipos activos y cambiarlos con sentencias directas. Trabaja
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
    public async Task DesactivarAsync(Guid equipoId, CancellationToken cancelacion = default)
    {
        await _contexto.JugadoresEquipo.Where(fila => fila.EquipoId == equipoId).ExecuteDeleteAsync(cancelacion);
        await _contexto.EntrenadoresEquipo.Where(fila => fila.EquipoId == equipoId).ExecuteDeleteAsync(cancelacion);
        await _contexto.Equipos
            .Where(equipo => equipo.Id == equipoId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(equipo => equipo.Activo, false), cancelacion);
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
