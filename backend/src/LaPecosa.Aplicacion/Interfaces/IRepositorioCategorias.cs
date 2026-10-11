using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las categorías del club de la petición, que fijó la autorización en
/// <see cref="IContextoClub"/>.
/// Su responsabilidad es leer las categorías con sus equipos activos y sus asignaciones activas,
/// contar sus jugadores y crear, activar, desactivar, marcar como usada o borrar una categoría.
/// No recibe un identificador de club ni ve categorías de otro club. No decide quién puede hacer
/// cada cosa ni cuándo se puede: eso lo hacen la autorización, las reglas del dominio y los
/// servicios.
/// </summary>
public interface IRepositorioCategorias
{
    /// <summary>
    /// Todas las categorías del club, activas e inactivas, por año, cada una con sus equipos
    /// activos (y los jugadores de cada uno) y sus asignaciones activas (con su integrante y los
    /// equipos que dirige). No las sigue.
    /// </summary>
    Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Una categoría del club, cargada igual que en la lista, o nulo si no existe en él.</summary>
    Task<Categoria?> ObtenerAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>
    /// Una categoría del club cargada igual que en la lista y, además, con la cuenta de cada
    /// entrenador asignado, para leer la versión de su foto (RF-014 de la 008); o nulo si no existe.
    /// </summary>
    Task<Categoria?> ObtenerConCuentasDeEntrenadoresAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>La categoría de ese año, activa o inactiva, sin sus equipos ni asignaciones.</summary>
    Task<Categoria?> BuscarPorAnioAsync(int anio, CancellationToken cancelacion = default);

    /// <summary>Número de jugadores de cada categoría que tiene alguno; cada jugador cuenta una vez.</summary>
    Task<IReadOnlyDictionary<Guid, int>> ContarJugadoresAsync(CancellationToken cancelacion = default);

    /// <summary>Número de jugadores de una categoría.</summary>
    Task<int> ContarJugadoresAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>Guarda de inmediato una categoría nueva.</summary>
    Task AgregarAsync(Categoria categoria, CancellationToken cancelacion = default);

    /// <summary>Deja la categoría activa o inactiva.</summary>
    Task CambiarActivaAsync(Guid categoriaId, bool activa, CancellationToken cancelacion = default);

    /// <summary>Marca que la categoría ya tuvo un jugador o un entrenador; no se desmarca nunca.</summary>
    Task MarcarUsadaAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>
    /// Desactiva todas las asignaciones de la categoría y borra los equipos que dirigían (RF-006).
    /// Las asignaciones no se borran.
    /// </summary>
    Task RetirarEntrenadoresAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra la categoría, y con ella sus equipos, solo si nunca se usó. Devuelve si la borró.
    /// </summary>
    Task<bool> BorrarSiNuncaSeUsoAsync(Guid categoriaId, CancellationToken cancelacion = default);
}
