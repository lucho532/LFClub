using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los equipos de las categorías del club de la petición.
/// Su responsabilidad es leer los equipos activos de una categoría, crearlos, renombrarlos,
/// desactivarlos o borrarlos, y poner o sacar a un jugador de un equipo.
/// No recibe un identificador de club ni ve equipos de otro club, y nunca devuelve un equipo
/// inactivo. No comprueba que el jugador sea de la categoría del equipo: eso lo hace el servicio.
/// </summary>
public interface IRepositorioEquipos
{
    /// <summary>Un equipo activo de esa categoría, o nulo si no existe, está inactivo o es de otra.</summary>
    Task<Equipo?> ObtenerActivoAsync(Guid categoriaId, Guid equipoId, CancellationToken cancelacion = default);

    /// <summary>Identificadores de los equipos activos de una categoría.</summary>
    Task<IReadOnlyList<Guid>> ListarActivosAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si la categoría ya tiene otro equipo activo con ese nombre normalizado, sin contar
    /// el equipo indicado.
    /// </summary>
    Task<bool> ExisteNombreAsync(
        Guid categoriaId, string nombreNormalizado, Guid? exceptoEquipoId, CancellationToken cancelacion = default);

    /// <summary>Guarda de inmediato un equipo nuevo.</summary>
    Task AgregarAsync(Equipo equipo, CancellationToken cancelacion = default);

    /// <summary>Cambia el nombre de un equipo.</summary>
    Task RenombrarAsync(Guid equipoId, string nombre, string nombreNormalizado, CancellationToken cancelacion = default);

    /// <summary>
    /// Desactiva el equipo y borra quién juega en él y quién lo dirige (RF-030). Los jugadores y
    /// los entrenadores siguen en la categoría.
    /// </summary>
    Task DesactivarAsync(Guid equipoId, CancellationToken cancelacion = default);

    /// <summary>Borra el equipo solo si nunca se usó. Devuelve si lo borró.</summary>
    Task<bool> BorrarSiNuncaSeUsoAsync(Guid equipoId, CancellationToken cancelacion = default);

    /// <summary>Marca que esos equipos ya tuvieron un jugador o un entrenador.</summary>
    Task MarcarUsadosAsync(IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default);

    /// <summary>Pone al jugador en el equipo; si ya estaba, no hace nada.</summary>
    Task PonerJugadorAsync(Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Saca al jugador del equipo; si no estaba, no hace nada.</summary>
    Task SacarJugadorAsync(Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion = default);
}
