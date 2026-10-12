namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador que marcó goles: en un partido jugado o en los goleadores de una
/// categoría (spec 009, RF-019 y RF-029).
/// Su responsabilidad es llevar solo su nombre, sus goles y, si quien mira puede abrir su ficha, el
/// enlace a ella (spec 008).
/// No lleva documento, fecha de nacimiento, categoría actual ni ningún otro dato del jugador.
/// </summary>
/// <param name="Nombres">Nombres del jugador.</param>
/// <param name="Apellidos">Apellidos del jugador.</param>
/// <param name="Goles">Goles; al menos uno.</param>
/// <param name="Enlace">Su ficha, si quien mira puede abrirla; nulo para la cuenta de un jugador.</param>
public record GoleadorDePartidoDto(string Nombres, string Apellidos, int Goles, EnlaceFichaDto? Enlace);
