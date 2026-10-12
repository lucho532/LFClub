namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa una página de comunicados publicados (spec 011, research §9).
/// Su responsabilidad es llevar los comunicados del más reciente al más antiguo y si hay más.
/// No lleva total: se pagina por cursor.
/// </summary>
/// <param name="Items">Comunicados de la página.</param>
/// <param name="HayMas">Si hay comunicados más antiguos.</param>
public record PaginaDePublicadosDto(IReadOnlyList<ComunicadoPublicadoDto> Items, bool HayMas);
