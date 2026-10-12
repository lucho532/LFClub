namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la corrección de la identidad de un jugador cuando el club se equivocó al
/// registrarla.
/// Su responsabilidad es llevar los nombres, los apellidos y la fecha de nacimiento corregidos.
/// No lleva el documento, que tiene su propia operación, ni ningún dato de contacto o de salud.
/// </summary>
/// <param name="Nombres">Nombres, hasta 80 caracteres.</param>
/// <param name="Apellidos">Apellidos, hasta 80 caracteres.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento; no puede ser futura.</param>
public record CorregirIdentidadDto(string? Nombres, string? Apellidos, DateOnly? FechaNacimiento);
