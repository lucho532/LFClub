namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las cifras generales del club (spec 015, RF-031 a RF-034; research §15).
/// Su responsabilidad es leer, con consultas agrupadas y sin tablas de totales, lo aplicado por pagos
/// confirmados en un mes (por la categoría que tenía el jugador cuando se generó cada cargo) y los
/// nombres de los jugadores y las categorías que las cifras mencionan. Los cargos con su pendiente los
/// da <see cref="IRepositorioCargos"/>.
/// No decide quién las ve ni calcula saldos o estados: eso lo hace el servicio con las reglas.
/// </summary>
public interface IRepositorioFinanzas
{
    /// <summary>
    /// Lo aplicado por pagos confirmados (no anulados) cuya confirmación cae en ese rango de tiempo UTC,
    /// una fila por aplicación con la categoría del cargo cubierto. Del club de la petición.
    /// </summary>
    Task<IReadOnlyList<AplicacionDelMes>> AplicacionesConfirmadasAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancelacion = default);

    /// <summary>Los jugadores con ese identificador, con su nombre y si siguen activos; también retirados.</summary>
    Task<IReadOnlyDictionary<Guid, JugadorDeFinanzas>> JugadoresAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);

    /// <summary>El año de cada una de esas categorías.</summary>
    Task<IReadOnlyDictionary<Guid, int>> AniosDeCategoriasAsync(
        IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default);
}

/// <summary>Lo que un pago confirmado aplicó a un cargo, con la categoría con la que se generó el cargo.</summary>
/// <param name="CategoriaId">Categoría del cargo; nula si no tenía.</param>
/// <param name="Valor">Valor aplicado.</param>
public record AplicacionDelMes(Guid? CategoriaId, decimal Valor);

/// <summary>Un jugador tal como lo mencionan las finanzas.</summary>
/// <param name="Nombre">Nombres y apellidos.</param>
/// <param name="Activo">Falso si está retirado del club.</param>
public record JugadorDeFinanzas(string Nombre, bool Activo);
