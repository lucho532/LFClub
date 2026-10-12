using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las asignaciones de entrenadores a las categorías del club de la
/// petición.
/// Su responsabilidad es leer quién entrena qué, asignar y retirar a un integrante de una
/// categoría y reemplazar los equipos que dirige.
/// No recibe un identificador de club ni ve asignaciones de otro club. No decide quién es
/// asignable ni cambia el rol de nadie, y nunca borra una asignación: la desactiva (RF-021).
/// </summary>
public interface IRepositorioAsignaciones
{
    /// <summary>La asignación de ese integrante a esa categoría, activa o no, o nulo si nunca existió.</summary>
    Task<AsignacionEntrenadorCategoria?> ObtenerAsync(
        Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Indica si el integrante tiene una asignación activa en esa categoría.</summary>
    Task<bool> TieneActivaAsync(Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Categorías en las que el integrante tiene una asignación activa.</summary>
    Task<IReadOnlyList<Guid>> CategoriasDeAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Integrantes del club con el ingreso aprobado y no retirados, de cualquier rol.</summary>
    Task<IReadOnlyList<UsuarioRol>> ListarIntegrantesAprobadosAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Asigna al integrante a la categoría: crea la asignación o vuelve a activar la que ya
    /// existía para esa pareja. Si ya estaba activa, no hace nada (RF-020).
    /// </summary>
    Task AsignarAsync(Guid categoriaId, Guid usuarioRolId, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Retira la asignación: la desactiva y borra los equipos que dirigía. Si no estaba asignado,
    /// no hace nada.
    /// </summary>
    Task RetirarAsync(Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Deja a la asignación dirigiendo exactamente esos equipos.</summary>
    Task ReemplazarEquiposAsync(
        Guid asignacionId, IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default);
}
