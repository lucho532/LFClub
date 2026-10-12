namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos para crear un club.
/// Su responsabilidad es llevar el nombre del club y el correo de su presidente, que es obligatorio (constitución §12.5).
/// No lleva colores, escudo ni estado: el club nace activo y con identidad neutra.
/// </summary>
/// <param name="Nombre">Nombre del club, de hasta 120 caracteres.</param>
/// <param name="CorreoPresidente">Correo al que se envía la invitación de presidente.</param>
public record CrearClubDto(
    string? Nombre,
    string? CorreoPresidente);
