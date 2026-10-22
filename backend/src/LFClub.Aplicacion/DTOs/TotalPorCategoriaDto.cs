namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el recaudo y la cartera de una categoría (spec 015, RF-032).
/// Su responsabilidad es llevar los dos totales de la categoría que tenía el jugador cuando se generó
/// cada cargo (§13), de modo que un cambio de categoría no reordena el pasado.
/// No lleva jugadores.
/// </summary>
/// <param name="CategoriaId">Categoría; nula es "Sin categoría".</param>
/// <param name="Nombre">Nombre de la categoría.</param>
/// <param name="RecaudoMes">Recaudado en el mes.</param>
/// <param name="Cartera">Pendiente de cobro.</param>
public record TotalPorCategoriaDto(Guid? CategoriaId, string Nombre, decimal RecaudoMes, decimal Cartera);
