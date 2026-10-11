namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa lo que quien pregunta puede hacer con un entrenamiento o un partido (spec 009).
/// Su responsabilidad es que la pantalla muestre o esconda sus acciones sin decidir por el rol.
/// No es la barrera: la API comprueba igualmente cada operación (§15).
/// </summary>
/// <param name="Gestiona">Cambiar, cancelar, reactivar, convocar y registrar el resultado.</param>
/// <param name="VePlanilla">Pedir la planilla con los jugadores, sus respuestas y su asistencia o lo que jugaron.</param>
/// <param name="Responde">Es la cuenta de un jugador que todavía puede decir si asistirá.</param>
public record PermisosDeActividadDto(bool Gestiona, bool VePlanilla, bool Responde);
