namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el PRESIDENTE al confirmar un pago en revisión (spec 015, RF-022).
/// Su responsabilidad es llevar, opcionalmente, el valor corregido y los cargos que cubre.
/// No valida nada: lo hacen <c>ValidadorPago</c> y la regla de aplicación.
/// </summary>
/// <param name="Valor">Valor corregido; si difiere de lo informado, este queda como valor informado.</param>
/// <param name="CargoIds">Cargos que cubre; sin ellos cubre los más antiguos primero.</param>
public record ConfirmarPagoDto(decimal? Valor, IReadOnlyList<Guid>? CargoIds);
