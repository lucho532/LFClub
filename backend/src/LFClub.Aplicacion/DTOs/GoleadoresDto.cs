using System.Text.Json.Serialization;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los goleadores del club en una categoría (spec 009, RF-029).
/// Su responsabilidad es llevar las categorías que tienen goles, para elegir, y, si se eligió una,
/// sus goleadores de mayor a menor con solo su nombre, sus goles y el enlace a su ficha.
/// No lleva ningún otro dato de los jugadores.
/// </summary>
/// <param name="CategoriasConGoles">Categorías con al menos un gol, por año descendente.</param>
/// <param name="Categoria">La elegida; ausente si no se eligió ninguna.</param>
/// <param name="Goleadores">De mayor a menor; vacía si no se eligió categoría.</param>
public record GoleadoresDto(
    IReadOnlyList<CategoriaDeActividadDto> CategoriasConGoles,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] CategoriaDeActividadDto? Categoria,
    IReadOnlyList<GoleadorDePartidoDto> Goleadores);
