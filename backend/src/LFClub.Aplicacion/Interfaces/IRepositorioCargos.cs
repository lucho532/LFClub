using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los cargos de los jugadores (spec 015).
/// Su responsabilidad es generar la mensualidad de cada mes de un solo golpe y sin duplicados, y
/// consultar los cargos de un jugador con lo que se les ha aplicado.
/// No decide quién puede ver o cambiar qué, ni calcula el estado de cuenta: eso lo hacen los servicios
/// y <c>ReglaEstadoDeCuenta</c>.
/// </summary>
public interface IRepositorioCargos
{
    /// <summary>
    /// Genera la mensualidad del mes en curso (hora de Colombia) de todos los jugadores que la deben,
    /// de todos los clubes, con una sola sentencia idempotente. Devuelve cuántos cargos creó. Es una
    /// operación del sistema: no devuelve datos de ningún club (constitución §7.1, nota 3 del plan).
    /// </summary>
    Task<int> GenerarDelMesAsync(DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Igual que <see cref="GenerarDelMesAsync"/> pero solo para un club, para cuando el PRESIDENTE fija
    /// el valor de la mensualidad por primera vez.
    /// </summary>
    Task<int> GenerarDelMesDelClubAsync(Guid clubId, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Los jugadores del club con un valor de mensualidad propio (spec 015, RF-012): aprobados y
    /// activos, con ese valor distinto de nulo. Del club de la petición.
    /// </summary>
    Task<IReadOnlyList<UsuarioRol>> ListarConValorPropioAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Fija el valor propio de esos jugadores, o lo quita con <paramref name="valor"/> nulo. Solo
    /// cambia a los jugadores aprobados y activos del club de la petición, y devuelve cuántos cambió
    /// para que quien llama compruebe que fueron todos.
    /// </summary>
    Task<int> FijarValorPropioAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, decimal? valor, CancellationToken cancelacion = default);

    /// <summary>
    /// Los cargos del club de la petición con lo aplicado a cada uno por pagos confirmados, o solo los
    /// de un jugador. Una sola consulta: con ellos se calculan los estados de cuenta de toda la lista
    /// sin preguntar fila por fila. Del más reciente al más antiguo.
    /// </summary>
    Task<IReadOnlyList<CargoConAplicado>> ListarConAplicadoAsync(
        Guid? usuarioRolId = null, CancellationToken cancelacion = default);
}
