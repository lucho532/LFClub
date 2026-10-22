using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios.Plataforma;

/// <summary>
/// Representa el conteo de integrantes activos por rol que sirve de base al cobro de la plataforma
/// (spec 017, RF-003).
/// Su responsabilidad es contar, saltándose el filtro de aislamiento porque el cobro y el trabajo
/// recorren todos los clubes, los integrantes de ingreso aprobado y no retirados, con la misma
/// definición de «activo» que <see cref="RepositorioClubesPlataforma"/>.
/// No lee datos personales: solo devuelve números.
/// </summary>
public class RepositorioConteoDeIntegrantes : IRepositorioConteoDeIntegrantes
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioConteoDeIntegrantes(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    private IQueryable<UsuarioRol> Activos => _contexto.UsuariosRol
        .IgnoreQueryFilters()
        .Where(integrante => integrante.EstadoIngreso == EstadoIngreso.APROBADO && integrante.Activo);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ConteoPorRol>> ContarPorRolDeClubesAsync(
        IEnumerable<Guid> clubIds, CancellationToken cancelacion = default)
    {
        var ids = clubIds.ToList();
        var filas = await Activos
            .Where(integrante => ids.Contains(integrante.ClubId))
            .GroupBy(integrante => new { integrante.ClubId, integrante.Rol })
            .Select(grupo => new FilaDeConteo(grupo.Key.ClubId, grupo.Key.Rol, grupo.Count()))
            .ToListAsync(cancelacion);

        return ids.Distinct().ToDictionary(
            id => id,
            id => new ConteoPorRol(
                Cantidad(filas, id, Rol.JUGADOR),
                Cantidad(filas, id, Rol.ENTRENADOR),
                Cantidad(filas, id, Rol.DIRECTIVO),
                Cantidad(filas, id, Rol.PRESIDENTE)));
    }

    /// <inheritdoc />
    public async Task<ConteoPorRol> ContarPorRolAsync(Guid clubId, CancellationToken cancelacion = default) =>
        (await ContarPorRolDeClubesAsync([clubId], cancelacion))[clubId];

    private static int Cantidad(IEnumerable<FilaDeConteo> filas, Guid clubId, Rol rol) =>
        filas.Where(fila => fila.ClubId == clubId && fila.Rol == rol).Select(fila => fila.Cantidad).FirstOrDefault();

    private sealed record FilaDeConteo(Guid ClubId, Rol Rol, int Cantidad);
}
