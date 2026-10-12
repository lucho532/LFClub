namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la respuesta de un inicio de sesión o de una renovación.
/// Su responsabilidad es entregar el token y cuándo vence.
/// No contiene datos de la cuenta, del club ni del rol.
/// </summary>
/// <param name="Token">Token de sesión.</param>
/// <param name="VenceEn">Fecha y hora, en UTC, en que deja de servir.</param>
public record TokenSesionDto(
    string Token,
    DateTime VenceEn);
