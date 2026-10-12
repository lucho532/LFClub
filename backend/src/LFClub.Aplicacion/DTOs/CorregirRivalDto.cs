namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se envía al corregir el rival de un partido ya jugado.
/// Su responsabilidad es llevar el nombre del rival, o nada si queda por definir.
/// No valida nada: lo hace el servicio.
/// </summary>
/// <param name="Rival">Equipo rival; nulo o vacío si queda por definir.</param>
public record CorregirRivalDto(string? Rival);
