namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una página de la campana de una cuenta (spec 012, RF-018 a RF-020; research §3).
/// Su responsabilidad es llevar comunicados y avisos mezclados del más reciente al más antiguo, si
/// hay más y el contador de sin leer, para no pedirlo aparte al abrir la lista.
/// No lleva total: se pagina por cursor.
/// </summary>
/// <param name="Elementos">Elementos de la página.</param>
/// <param name="HayMas">Si hay elementos más antiguos de cualquiera de los dos tipos.</param>
/// <param name="SinLeer">Comunicados más avisos sin leer de la cuenta, cada uno una sola vez.</param>
public record PaginaDeCampanaDto(IReadOnlyList<ElementoDeCampanaDto> Elementos, bool HayMas, int SinLeer);
