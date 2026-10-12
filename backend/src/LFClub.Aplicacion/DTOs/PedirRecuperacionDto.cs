namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la petición del correo de recuperación de contraseña.
/// Su responsabilidad es llevar el correo de la cuenta.
/// No revela si el correo existe: la respuesta es siempre la misma.
/// </summary>
/// <param name="Correo">Correo de la cuenta.</param>
public record PedirRecuperacionDto(
    string? Correo);
