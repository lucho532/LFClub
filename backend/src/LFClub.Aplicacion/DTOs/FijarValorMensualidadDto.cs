namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el PRESIDENTE para fijar el valor de la mensualidad del club (spec 015,
/// RF-011).
/// Su responsabilidad es llevar el valor.
/// No valida nada: lo hace <c>ValidadorPago</c> (entero de pesos y mayor que cero).
/// </summary>
/// <param name="Valor">Valor en pesos enteros.</param>
public record FijarValorMensualidadDto(decimal Valor);
