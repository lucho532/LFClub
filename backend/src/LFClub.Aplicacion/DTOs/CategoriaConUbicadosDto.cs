namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el resultado de crear o reactivar una categoría (RF-010).
/// Su responsabilidad es devolver la categoría e informar de cuántos jugadores sin categoría
/// nacidos ese año entraron solos en ella.
/// No dice quiénes fueron: eso se ve en el detalle de la categoría.
/// </summary>
/// <param name="Categoria">La categoría creada o reactivada.</param>
/// <param name="JugadoresUbicados">Cuántos jugadores entraron; puede ser 0.</param>
public record CategoriaConUbicadosDto(CategoriaDto Categoria, int JugadoresUbicados);
