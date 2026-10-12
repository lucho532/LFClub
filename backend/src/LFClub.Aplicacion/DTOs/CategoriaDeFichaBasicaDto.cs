namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una categoría que entrena la persona de una ficha básica (spec 008, RF-001).
/// Su responsabilidad es llevar el año de la categoría y los nombres de los equipos activos que
/// dirige en ella.
/// No lleva identificadores de categoría ni de equipo: quien ve la ficha básica, también la familia,
/// no los necesita.
/// </summary>
/// <param name="Anio">Año de la categoría.</param>
/// <param name="Equipos">Nombres de los equipos que dirige, por nombre; vacía si lo es de la categoría en general.</param>
public record CategoriaDeFichaBasicaDto(int Anio, IReadOnlyList<string> Equipos);
