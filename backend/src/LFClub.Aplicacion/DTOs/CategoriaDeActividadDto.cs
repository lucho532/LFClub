namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una categoría de un entrenamiento o de un partido, o la copiada en un registro
/// deportivo (spec 009).
/// Su responsabilidad es llevar su identificador y su año, que es su nombre.
/// No dice si la categoría sigue activa ni cuántos jugadores tiene.
/// </summary>
/// <param name="CategoriaId">Identificador de la categoría.</param>
/// <param name="Anio">Año de la categoría.</param>
public record CategoriaDeActividadDto(Guid CategoriaId, int Anio);
