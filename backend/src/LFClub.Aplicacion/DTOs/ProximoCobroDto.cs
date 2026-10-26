namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el siguiente cobro que le llegará al club, todavía sin generar (spec 017).
/// Su responsabilidad es llevar al PRESIDENTE el mes, los jugadores de hoy, el rango de la tarifa vigente y
/// el valor estimado. Es solo una estimación: el cobro real se genera al empezar el mes con los jugadores
/// y la tarifa de ese momento.
/// No es un cobro y no se puede pagar.
/// </summary>
/// <param name="Periodo">Mes que cobrará, <c>AAAA-MM</c>.</param>
/// <param name="Jugadores">Jugadores activos del club hoy.</param>
/// <param name="Rango">Rango de la tarifa vigente que le corresponde hoy.</param>
/// <param name="ValorEstimado">Lo que pagaría con la tarifa y los jugadores de hoy, en pesos.</param>
public record ProximoCobroDto(string Periodo, int Jugadores, RangoTarifaDto Rango, decimal ValorEstimado);
