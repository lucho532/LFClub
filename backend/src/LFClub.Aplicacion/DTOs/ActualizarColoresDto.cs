namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los colores que el DESARROLLADOR define para un club (RF-008).
/// Su responsabilidad es llevar el color principal y el de acento, en formato #RRGGBB.
/// No lleva el escudo ni decide el color del texto: eso lo calcula la interfaz según el contraste.
/// </summary>
/// <param name="ColorPrincipal">Color principal, en formato #RRGGBB.</param>
/// <param name="ColorAcento">Color de acento, en formato #RRGGBB.</param>
public record ActualizarColoresDto(
    string? ColorPrincipal,
    string? ColorAcento);
