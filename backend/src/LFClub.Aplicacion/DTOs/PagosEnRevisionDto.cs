namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la lista de pagos que esperan revisión (spec 015, RF-022).
/// Su responsabilidad es llevar el total, que usa el contador del inicio del PRESIDENTE, y los pagos
/// del más antiguo al más reciente.
/// No es un recordatorio de cobro (§16.8).
/// </summary>
/// <param name="Total">Cantidad de pagos en revisión.</param>
/// <param name="Pagos">Los pagos.</param>
public record PagosEnRevisionDto(int Total, IReadOnlyList<PagoEnRevisionDto> Pagos);
