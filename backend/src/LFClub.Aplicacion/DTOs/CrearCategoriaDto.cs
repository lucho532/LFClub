namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos para crear una categoría (RF-001).
/// Su responsabilidad es llevar el año de nacimiento, que es lo único que define a una categoría.
/// No lleva el club, que sale de la ruta, ni un nombre libre, horario, sede o cupo.
/// </summary>
/// <param name="Anio">Año de nacimiento, de cuatro cifras y no posterior al año en curso.</param>
public record CrearCategoriaDto(int? Anio);
