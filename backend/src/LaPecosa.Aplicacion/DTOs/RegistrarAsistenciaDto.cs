namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa la asistencia que se registra o corrige en un entrenamiento (spec 009, RF-022).
/// Su responsabilidad es llevar, de cada jugador enviado, si estuvo presente. Las filas no enviadas
/// no cambian.
/// No valida nada: lo hace el servicio.
/// </summary>
/// <param name="Jugadores">Al menos un jugador.</param>
public record RegistrarAsistenciaDto(IReadOnlyList<AsistenciaDeJugadorDto>? Jugadores);

/// <summary>
/// Representa la asistencia de un jugador.
/// Su responsabilidad es decir si estuvo presente.
/// No lleva su categoría, que se copia la primera vez.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="Presente">Presente o ausente.</param>
public record AsistenciaDeJugadorDto(Guid UsuarioRolId, bool? Presente);
