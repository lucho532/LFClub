namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la identidad visual de un club.
/// Su responsabilidad es llevar sus colores y la dirección de su escudo.
/// No contiene la imagen: todos los campos nulos significan identidad neutra de la plataforma.
/// </summary>
/// <param name="ColorPrincipal">Color principal en formato #RRGGBB, o nulo.</param>
/// <param name="ColorAcento">Color de acento en formato #RRGGBB, o nulo.</param>
/// <param name="UrlEscudo">Dirección del escudo con su versión, o nulo si no tiene.</param>
public record IdentidadClubDto(
    string? ColorPrincipal,
    string? ColorAcento,
    string? UrlEscudo);
