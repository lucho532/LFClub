namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la creación de una contraseña nueva con el enlace recibido por correo.
/// Su responsabilidad es llevar el token del enlace y la contraseña nueva.
/// No lleva el correo ni la contraseña anterior: el token identifica a la cuenta.
/// </summary>
/// <param name="Token">Token del enlace de recuperación.</param>
/// <param name="ContrasenaNueva">Contraseña nueva, de 8 a 128 caracteres.</param>
public record RestablecerContrasenaDto(
    string? Token,
    string? ContrasenaNueva);
