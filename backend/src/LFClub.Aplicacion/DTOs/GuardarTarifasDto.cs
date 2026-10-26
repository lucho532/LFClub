namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el DESARROLLADOR para fijar los rangos de la tarifa y los días de gracia
/// (spec 017, RF-001 y RF-002).
/// Su responsabilidad es llevar los valores; los rangos van ordenados, seguidos, desde 1 y el último
/// sin límite superior.
/// No valida nada: lo hace <c>ServicioTarifas</c>.
/// </summary>
/// <param name="Rangos">Rangos de jugadores con su precio mensual fijo; vacío = no se cobra.</param>
/// <param name="DiasDeGracia">Días de gracia, de 0 a 30.</param>
public record GuardarTarifasDto(IReadOnlyList<RangoTarifaDto> Rangos, int DiasDeGracia);
