using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el dibujo de la carta de un jugador como imagen PNG (spec 010, historia 4; research
/// §11).
/// Su responsabilidad es convertir los datos de la carta, ya calculados y ya autorizados, en una
/// imagen de 750 × 1050 que sigue el plano común con la pantalla (contracts/carta.md).
/// No decide quién exporta, no consulta nada y no añade ningún dato que no reciba (RF-030).
/// </summary>
public interface IGeneradorImagenCarta
{
    /// <summary>Los bytes del PNG de la carta.</summary>
    byte[] Generar(DatosImagenCarta datos);
}

/// <summary>
/// Representa lo que se dibuja en la imagen de la carta: lo mismo que la carta en pantalla y nada más
/// (RF-030).
/// Su responsabilidad es llevarlo junto, con la foto y el escudo como bytes.
/// No lleva documento, fecha de nacimiento, contacto, salud, categoría ni porcentaje de asistencia.
/// </summary>
/// <param name="Nombres">Nombres del jugador.</param>
/// <param name="Apellidos">Apellidos del jugador.</param>
/// <param name="Dorsal">Dorsal, o nulo.</param>
/// <param name="Posicion">Posición, o nula.</param>
/// <param name="Estadisticas">Las seis estadísticas, o nulas.</param>
/// <param name="Valoracion">Valoración, o nula sin estadísticas.</param>
/// <param name="Rareza">Rareza por asistencia.</param>
/// <param name="Foto">Bytes de la foto del jugador (PNG, JPEG o WebP), o nulos: figura genérica.</param>
/// <param name="Escudo">Bytes del escudo del club, o nulos.</param>
/// <param name="ColorPrincipal">Color principal del club (#RRGGBB), o nulo.</param>
/// <param name="ColorAcento">Color de acento del club (#RRGGBB), o nulo.</param>
public record DatosImagenCarta(
    string Nombres,
    string Apellidos,
    int? Dorsal,
    PosicionJugador? Posicion,
    ValoresCartaDto? Estadisticas,
    int? Valoracion,
    RarezaCarta Rareza,
    byte[]? Foto,
    byte[]? Escudo,
    string? ColorPrincipal,
    string? ColorAcento);
