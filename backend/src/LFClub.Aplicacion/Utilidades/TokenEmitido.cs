namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa un token de sesión recién emitido.
/// Su responsabilidad es llevar el token y el momento en que vence.
/// No es un DTO de la API ni contiene datos del club o del rol.
/// </summary>
/// <param name="Token">Token firmado.</param>
/// <param name="VenceEn">Fecha y hora, en UTC, en que deja de servir.</param>
public record TokenEmitido(string Token, DateTime VenceEn);
