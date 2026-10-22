namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el valor de la mensualidad del club y los jugadores con valor propio (spec 015,
/// RF-011 y RF-012).
/// Su responsabilidad es llevar el valor del club, nulo mientras el PRESIDENTE no lo defina, y quiénes
/// pagan otro valor.
/// No lleva cargos ni pagos.
/// </summary>
/// <param name="ValorClub">Valor del club en pesos enteros; nulo si aún no se definió.</param>
/// <param name="JugadoresConValorPropio">Jugadores activos con un valor distinto del del club.</param>
public record MensualidadDto(decimal? ValorClub, IReadOnlyList<JugadorConValorPropioDto> JugadoresConValorPropio);
