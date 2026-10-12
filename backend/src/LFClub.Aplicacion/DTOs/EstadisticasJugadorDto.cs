namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa las estadísticas de un jugador en su ficha (spec 009, RF-026 a RF-028).
/// Su responsabilidad es llevar el total y una fila por cada categoría en la que tiene registros,
/// por año descendente.
/// No llega a quien no puede ver la ficha del jugador: esa persona recibe el 404 de la ficha.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="Total">Suma de todas las categorías.</param>
/// <param name="PorCategoria">Por categoría copiada, por año descendente; vacía sin registros.</param>
public record EstadisticasJugadorDto(Guid UsuarioRolId, CifrasDto Total, IReadOnlyList<CifrasDeCategoriaDto> PorCategoria);
