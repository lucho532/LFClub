namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el DESARROLLADOR para fijar desde qué mes se cobra a un club (spec 017).
/// Su responsabilidad es llevar el mes en formato <c>AAAA-MM</c>, o nulo para no cobrar.
/// No valida el formato: lo hace <c>ServicioInicioDelCobro</c>.
/// </summary>
/// <param name="MesDeInicio">Mes <c>AAAA-MM</c> desde el que se cobra; nulo para no cobrar.</param>
public record FijarInicioDelCobroDto(string? MesDeInicio);
