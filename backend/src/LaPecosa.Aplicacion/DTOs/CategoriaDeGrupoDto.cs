namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa la categoría de un grupo de la lista de jugadores (spec 007, RF-002).
/// Su responsabilidad es llevar lo que la pantalla necesita para titular el grupo y enlazarlo.
/// No lleva sus equipos, sus entrenadores ni cuántos jugadores tiene.
/// </summary>
/// <param name="CategoriaId">Identificador de la categoría.</param>
/// <param name="Anio">Año de nacimiento de la categoría.</param>
public record CategoriaDeGrupoDto(Guid CategoriaId, int Anio);
