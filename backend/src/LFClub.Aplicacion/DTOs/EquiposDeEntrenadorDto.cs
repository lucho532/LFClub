namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la lista completa de equipos de una categoría que dirige un entrenador (RF-028).
/// Su responsabilidad es reemplazar la lista anterior; vacía significa que es entrenador de la
/// categoría en general.
/// No añade ni quita equipos de uno en uno.
/// </summary>
/// <param name="EquipoIds">Equipos activos de la categoría que dirige; puede ir vacía.</param>
public record EquiposDeEntrenadorDto(IReadOnlyList<Guid>? EquipoIds);
