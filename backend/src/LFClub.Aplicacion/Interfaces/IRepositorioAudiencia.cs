using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a la audiencia de los entrenamientos del club de la petición (spec 013,
/// research §3, §6 y §9; data-model "Cálculos").
/// Su responsabilidad es, con consultas del club, calcular los jugadores activos de la audiencia de
/// un entrenamiento guardado o de uno que todavía no se guarda, validar qué equipos y qué jugadores
/// se pueden usar, buscar jugadores que se pueden sumar y reemplazar los equipos y los añadidos de un
/// entrenamiento conservando los equipos desactivados y las filas del sistema.
/// Trabaja siempre con el filtro de aislamiento activo: nunca ve otro club. No decide quién ve el
/// entrenamiento, no abre transacciones y no genera avisos.
/// </summary>
public interface IRepositorioAudiencia
{
    /// <summary>De esos equipos, los que existen en el club, con su categoría y si siguen activos.</summary>
    Task<IReadOnlyList<EquipoParaAudiencia>> EquiposAsync(
        IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default);

    /// <summary>
    /// De esos integrantes, los jugadores que se pueden sumar: de rol JUGADOR, activos, aprobados, no
    /// retirados y con categoría.
    /// </summary>
    Task<IReadOnlySet<Guid>> JugadoresSumablesAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Los jugadores activos de la audiencia natural de un entrenamiento que todavía no está
    /// guardado: los de esos equipos, o los de esas categorías si no hay equipos.
    /// </summary>
    Task<IReadOnlySet<Guid>> AudienciaNaturalAsync(
        IReadOnlyCollection<Guid> categoriaIds, IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Los jugadores activos de la audiencia total de un entrenamiento guardado: los de sus equipos
    /// (o de sus categorías si no tiene equipos), más los de su lista guardada.
    /// </summary>
    Task<IReadOnlySet<Guid>> AudienciaDeAsync(Guid entrenamientoId, CancellationToken cancelacion = default);

    /// <summary>
    /// Busca jugadores activos del club con categoría por nombres o apellidos, ordenados por apellidos
    /// y nombres y con lo mínimo para elegirlos (research §9).
    /// </summary>
    Task<IReadOnlyList<JugadorSumableDto>> BuscarSumablesAsync(
        string texto, int limite, CancellationToken cancelacion = default);

    /// <summary>
    /// Los jugadores añadidos por una persona a un entrenamiento (origen <c>ANADIDO</c>), por
    /// apellidos. No incluye a los congelados por un equipo desactivado, que son audiencia natural
    /// (RF-021).
    /// </summary>
    Task<IReadOnlyList<JugadorSumableDto>> AnadidosAsync(Guid entrenamientoId, CancellationToken cancelacion = default);

    /// <summary>
    /// Reemplaza las filas de equipo del entrenamiento por <paramref name="equipoIds"/> (que ya
    /// incluye los desactivados conservados) y sus filas <c>ANADIDO</c> por
    /// <paramref name="anadidoIds"/>. No toca las filas <c>EQUIPO_DESACTIVADO</c>.
    /// </summary>
    Task ReemplazarAsync(
        Guid entrenamientoId,
        IReadOnlyCollection<Guid> equipoIds,
        IReadOnlyCollection<Guid> anadidoIds,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Borra la respuesta de esos jugadores al entrenamiento, salvo los que tienen asistencia
    /// registrada en él (siguen viéndolo).
    /// </summary>
    Task QuitarRespuestasAsync(
        Guid entrenamientoId, IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);
}

/// <summary>
/// Representa un equipo tal como lo necesita la validación de la audiencia.
/// Su responsabilidad es llevar su identificador, su categoría y si está activo.
/// No lleva a sus jugadores.
/// </summary>
/// <param name="EquipoId">Identificador del equipo.</param>
/// <param name="CategoriaId">Categoría del equipo.</param>
/// <param name="Activo">Si está activo.</param>
/// <param name="CategoriaActiva">Si su categoría está activa.</param>
public record EquipoParaAudiencia(Guid EquipoId, Guid CategoriaId, bool Activo, bool CategoriaActiva);
