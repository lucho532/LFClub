namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se envía al programar o cambiar un entrenamiento (spec 009, RF-001, RF-005).
/// Su responsabilidad es llevar los datos tal como llegan, en hora de Colombia: fecha
/// <c>AAAA-MM-DD</c> y horas <c>HH:mm</c>.
/// No valida nada: eso lo hace <c>ValidadorEntrenamiento</c>. Al cambiar, sus categorías deben ser
/// las que ya tiene.
/// </summary>
/// <param name="Fecha">Fecha del entrenamiento.</param>
/// <param name="HoraInicio">Hora de inicio.</param>
/// <param name="HoraFin">Hora de fin, posterior a la de inicio.</param>
/// <param name="Lugar">Lugar, de 120 caracteres como máximo.</param>
/// <param name="Nota">Nota opcional, de 500 caracteres como máximo.</param>
/// <param name="CategoriaIds">Una o más categorías, sin repetir.</param>
/// <param name="EquipoIds">
/// Equipos activos de esas categorías a los que va dirigido (spec 013, RF-001). Ausente o vacío: toda
/// la categoría. Al cambiar, deben ser los que ya tiene (RF-020 es de otra operación).
/// </param>
/// <param name="JugadorAnadidoIds">Jugadores de cualquier categoría del club que se suman (RF-013). Ausente: ninguno.</param>
public record GuardarEntrenamientoDto(
    string? Fecha,
    string? HoraInicio,
    string? HoraFin,
    string? Lugar,
    string? Nota,
    IReadOnlyList<Guid>? CategoriaIds,
    IReadOnlyList<Guid>? EquipoIds = null,
    IReadOnlyList<Guid>? JugadorAnadidoIds = null);
