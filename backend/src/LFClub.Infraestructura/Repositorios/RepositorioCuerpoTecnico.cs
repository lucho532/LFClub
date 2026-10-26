using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa la lectura del cuerpo técnico con Entity Framework (spec 018).
/// Su responsabilidad es leer a los integrantes que no son jugadores con los datos de su cuenta y sus
/// asignaciones activas. Trabaja siempre con el filtro de aislamiento activo: sin club en el contexto no
/// devuelve nada.
/// No se salta el filtro de aislamiento, no modifica datos ni contiene reglas de negocio.
/// </summary>
public class RepositorioCuerpoTecnico : IRepositorioCuerpoTecnico
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioCuerpoTecnico(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    private IQueryable<UsuarioRol> Equipo => _contexto.UsuariosRol
        .AsNoTracking()
        .Where(integrante => integrante.Rol != Rol.JUGADOR
            && integrante.EstadoIngreso == EstadoIngreso.APROBADO
            && integrante.Activo);

    /// <inheritdoc />
    public async Task<IReadOnlyList<MiembroConContacto>> ListarAsync(CancellationToken cancelacion = default)
    {
        var filas = await Equipo
            .Select(integrante => new
            {
                Integrante = integrante,
                integrante.Usuario!.Correo,
                integrante.Usuario.Celular,
                integrante.Usuario.VersionFoto,
            })
            .ToListAsync(cancelacion);

        return filas.Select(fila => new MiembroConContacto(fila.Integrante, fila.Correo, fila.Celular, fila.VersionFoto)).ToList();
    }

    /// <inheritdoc />
    public async Task<MiembroConContacto?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var fila = await Equipo
            .Where(integrante => integrante.Id == usuarioRolId)
            .Select(integrante => new
            {
                Integrante = integrante,
                integrante.Usuario!.Correo,
                integrante.Usuario.Celular,
                integrante.Usuario.VersionFoto,
            })
            .FirstOrDefaultAsync(cancelacion);

        return fila is null ? null : new MiembroConContacto(fila.Integrante, fila.Correo, fila.Celular, fila.VersionFoto);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AsignacionEntrenadorCategoria>> AsignacionesActivasAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default) =>
        await _contexto.AsignacionesEntrenadorCategoria
            .AsNoTracking()
            .Where(asignacion => usuarioRolIds.Contains(asignacion.UsuarioRolId)
                && asignacion.Activa
                && asignacion.Categoria!.Activa)
            .Include(asignacion => asignacion.Categoria)
            .Include(asignacion => asignacion.Equipos.Where(fila => fila.Equipo!.Activo))
                .ThenInclude(fila => fila.Equipo)
            .ToListAsync(cancelacion);
}
