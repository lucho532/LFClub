namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa las finanzas generales del club en un mes (spec 015, RF-031 a RF-034).
/// Su responsabilidad es llevar lo recaudado en el mes, la cartera pendiente, los totales por
/// categoría y quiénes deben.
/// No cuenta pagos en revisión, rechazados ni anulados, y no se guarda: se calcula de los cargos y los
/// pagos al pedirla.
/// </summary>
/// <param name="Mes">Primer día del mes consultado.</param>
/// <param name="RecaudoMes">Lo aplicado por pagos confirmados en el mes, en pesos.</param>
/// <param name="CarteraPendiente">Suma de los saldos de todos los jugadores del club.</param>
/// <param name="PagosEnRevision">Cantidad de pagos en revisión.</param>
/// <param name="PorCategoria">Recaudo y cartera por categoría del cargo.</param>
/// <param name="JugadoresQueDeben">Jugadores con saldo pendiente.</param>
public record FinanzasDto(
    DateOnly Mes,
    decimal RecaudoMes,
    decimal CarteraPendiente,
    int PagosEnRevision,
    IReadOnlyList<TotalPorCategoriaDto> PorCategoria,
    IReadOnlyList<JugadorQueDebeDto> JugadoresQueDeben);
