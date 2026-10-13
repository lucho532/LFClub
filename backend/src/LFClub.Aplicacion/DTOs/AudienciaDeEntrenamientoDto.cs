namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se envía al cambiar a quién va dirigido un entrenamiento (spec 013, RF-020).
/// Su responsabilidad es llevar los equipos activos y los jugadores añadidos, completos: lo que no
/// esté aquí sale. Los equipos desactivados que el entrenamiento ya tenía no se envían y se conservan
/// (RF-026).
/// No valida nada: eso lo hace <c>ArmadoDeAudiencia</c>.
/// </summary>
/// <param name="EquipoIds">Equipos activos, completos. Vacío deja el entrenamiento en toda la categoría.</param>
/// <param name="JugadorIds">Jugadores añadidos, completos.</param>
public record AudienciaDeEntrenamientoDto(IReadOnlyList<Guid>? EquipoIds, IReadOnlyList<Guid>? JugadorIds);
