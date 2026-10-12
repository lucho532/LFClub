namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el nombre de un equipo al crearlo o renombrarlo (RF-022 a RF-024).
/// Su responsabilidad es llevar ese nombre.
/// No lleva la categoría, que sale de la ruta y no se puede cambiar.
/// </summary>
/// <param name="Nombre">Nombre del equipo: obligatorio y de 30 caracteres como máximo.</param>
public record NombreEquipoDto(string? Nombre);
