namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa lo que ve una familia en el inicio de su club: la categoría de su jugador (RF-035).
/// Su responsabilidad es llevar esa categoría, o nulo si el jugador todavía no tiene.
/// No lleva ningún identificador ni datos de otros jugadores o de otras categorías.
/// </summary>
/// <param name="Categoria">La categoría del jugador; nulo si todavía no tiene.</param>
public record MiCategoriaDto(CategoriaDeMiJugadorDto? Categoria);

/// <summary>
/// Representa la categoría del jugador con el que continúa la cuenta, tal como la ve su familia.
/// Su responsabilidad es llevar el año, los nombres de los equipos en los que está y los
/// entrenadores de la categoría.
/// No lleva identificadores, el número de jugadores ni la lista de sus compañeros.
/// </summary>
/// <param name="Anio">Año de la categoría.</param>
/// <param name="Equipos">Nombres de los equipos en los que está el jugador.</param>
/// <param name="Entrenadores">Entrenadores de la categoría; vacía si todavía no tiene.</param>
public record CategoriaDeMiJugadorDto(
    int Anio, IReadOnlyList<string> Equipos, IReadOnlyList<EntrenadorParaFamiliaDto> Entrenadores);
