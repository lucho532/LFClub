namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa un grupo de la lista "Jugadores": los jugadores de una categoría o, con
/// <see cref="Categoria"/> nula, los que no tienen ninguna (spec 007, RF-002 y RF-004).
/// Su responsabilidad es llevar los jugadores del grupo ya ordenados.
/// No existe un grupo vacío: si no tiene jugadores, no se devuelve.
/// </summary>
/// <param name="Categoria">La categoría del grupo; nula en "Sin categoría".</param>
/// <param name="Jugadores">Los jugadores del grupo, por apellidos y nombres.</param>
public record GrupoDeJugadoresDto(CategoriaDeGrupoDto? Categoria, IReadOnlyList<JugadorDeListaDto> Jugadores);
