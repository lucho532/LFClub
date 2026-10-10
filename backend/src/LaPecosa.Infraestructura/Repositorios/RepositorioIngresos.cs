using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los ingresos del club de la petición con Entity Framework.
/// Su responsabilidad es leer la sala de espera (cada ingreso con su cuenta y con el jugador desde
/// cuya ficha se agregó) y los ingresos aprobados, y aprobar o borrar un
/// ingreso con una sentencia condicionada a que siga en espera. Trabaja siempre con el filtro de
/// aislamiento activo: sin club en el contexto no devuelve ni cambia nada.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioIngresos : IRepositorioIngresos
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioIngresos(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    private IQueryable<UsuarioRol> EnEspera => _contexto.UsuariosRol
        .Where(integrante => integrante.EstadoIngreso == EstadoIngreso.EN_ESPERA);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarEnEsperaAsync(CancellationToken cancelacion = default) =>
        await EnEspera
            .AsNoTracking()
            .Include(integrante => integrante.Usuario)
            .Include(integrante => integrante.AgregadoDesde)
            .OrderBy(integrante => integrante.CreadoEn)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarAprobadosAsync(CancellationToken cancelacion = default) =>
        await _contexto.UsuariosRol
            .AsNoTracking()
            .Where(integrante => integrante.AprobadoEn != null)
            .OrderByDescending(integrante => integrante.AprobadoEn)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<UsuarioRol?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .AsNoTracking()
            .Include(integrante => integrante.Usuario)
            .FirstOrDefaultAsync(integrante => integrante.Id == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public async Task<bool> AprobarAsync(
        Guid usuarioRolId,
        DateTime ahoraUtc,
        Guid aprobadoPorUsuarioId,
        string aprobadoPorNombre,
        CancellationToken cancelacion = default) =>
        await EnEspera
            .Where(integrante => integrante.Id == usuarioRolId)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(integrante => integrante.EstadoIngreso, EstadoIngreso.APROBADO)
                    .SetProperty(integrante => integrante.Rol, Rol.JUGADOR)
                    .SetProperty(integrante => integrante.RolDeIngreso, Rol.JUGADOR)
                    .SetProperty(integrante => integrante.AprobadoEn, ahoraUtc)
                    .SetProperty(integrante => integrante.AprobadoPorUsuarioId, aprobadoPorUsuarioId)
                    .SetProperty(integrante => integrante.AprobadoPorNombre, aprobadoPorNombre),
                cancelacion) == 1;

    /// <inheritdoc />
    public async Task<bool> BorrarSiSigueEnEsperaAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await EnEspera
            .Where(integrante => integrante.Id == usuarioRolId)
            .ExecuteDeleteAsync(cancelacion) == 1;
}
