using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa la lectura de la ficha básica con Entity Framework.
/// Su responsabilidad es hacer las dos consultas de la ficha básica: el integrante con la versión de
/// la foto de su cuenta, y sus asignaciones activas con sus equipos activos. Trabaja siempre con el
/// filtro de aislamiento activo: sin club en el contexto no devuelve nada.
/// No se salta el filtro de aislamiento, no modifica datos ni contiene reglas de negocio.
/// </summary>
public class RepositorioFichaBasica : IRepositorioFichaBasica
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioFichaBasica(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<PersonaConFoto?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var fila = await _contexto.UsuariosRol
            .AsNoTracking()
            .Where(integrante => integrante.Id == usuarioRolId
                && integrante.Rol != Rol.JUGADOR
                && integrante.EstadoIngreso == EstadoIngreso.APROBADO)
            .Select(integrante => new { Integrante = integrante, integrante.Usuario!.VersionFoto })
            .FirstOrDefaultAsync(cancelacion);

        return fila is null ? null : new PersonaConFoto(fila.Integrante, fila.VersionFoto);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AsignacionEntrenadorCategoria>> AsignacionesActivasAsync(
        Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.AsignacionesEntrenadorCategoria
            .AsNoTracking()
            .Where(asignacion => asignacion.UsuarioRolId == usuarioRolId
                && asignacion.Activa
                && asignacion.Categoria!.Activa)
            .Include(asignacion => asignacion.Categoria)
            .Include(asignacion => asignacion.Equipos.Where(fila => fila.Equipo!.Activo))
                .ThenInclude(fila => fila.Equipo)
            .ToListAsync(cancelacion);
}
