namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el token de un enlace de invitación.
/// Su responsabilidad es llevarlo en el cuerpo de la petición, porque en el enlace viaja en el fragmento y no llega al servidor.
/// No identifica a ninguna cuenta.
/// </summary>
/// <param name="Token">Token del enlace.</param>
public record TokenDto(
    string? Token);
