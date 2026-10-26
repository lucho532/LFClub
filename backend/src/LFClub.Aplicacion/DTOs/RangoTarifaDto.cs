namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un rango de la tarifa de la plataforma (spec 017, RF-001).
/// Su responsabilidad es llevar desde cuántos jugadores, hasta cuántos y el precio mensual fijo.
/// No valida nada: lo hace <c>ServicioTarifas</c>.
/// </summary>
/// <param name="Desde">Primer jugador del rango (1 o más).</param>
/// <param name="Hasta">Último jugador del rango; nulo si no tiene límite (solo el último rango).</param>
/// <param name="Precio">Precio mensual fijo del rango, en pesos enteros.</param>
public record RangoTarifaDto(int Desde, int? Hasta, decimal Precio);
