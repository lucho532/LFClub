namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el motivo con el que el PRESIDENTE rechaza o anula un pago (spec 015, RF-019 y RF-022).
/// Su responsabilidad es llevar el texto.
/// No valida nada: lo hace <c>ValidadorPago</c> (obligatorio, hasta 300 caracteres).
/// </summary>
/// <param name="Motivo">Motivo, que la familia ve en un rechazo.</param>
public record MotivoDto(string? Motivo);
