namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el contacto de emergencia de un jugador: la persona a la que se llama si le pasa
/// algo.
/// Su responsabilidad es llevar su nombre, su parentesco y su celular, los tres opcionales.
/// No es un dato clínico: lo ve todo el que puede ver la ficha, también un DIRECTIVO.
/// </summary>
/// <param name="Nombre">Nombre de la persona.</param>
/// <param name="Parentesco">Parentesco con el jugador.</param>
/// <param name="Celular">Celular de la persona.</param>
public record ContactoEmergenciaDto(string? Nombre, string? Parentesco, string? Celular);
