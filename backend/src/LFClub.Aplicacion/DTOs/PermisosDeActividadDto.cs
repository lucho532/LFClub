namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que quien pregunta puede hacer con un entrenamiento o un partido (spec 009).
/// Su responsabilidad es que la pantalla muestre o esconda sus acciones sin decidir por el rol.
/// No es la barrera: la API comprueba igualmente cada operación (§15).
/// </summary>
/// <param name="Gestiona">Cambiar, cancelar, reactivar, convocar y registrar el resultado.</param>
/// <param name="VePlanilla">Pedir la planilla con los jugadores, sus respuestas y su asistencia o lo que jugaron.</param>
/// <param name="Responde">Es la cuenta de un jugador que todavía puede decir si asistirá.</param>
/// <param name="IndicaResultado">
/// Es la cuenta de un convocado a un partido que ya empezó, no está cancelado y cuya fila no
/// corrigió quien entrena: puede publicar el resultado o indicar lo de su jugador (spec 010).
/// Siempre falso en los entrenamientos.
/// </param>
/// <param name="Dirige">
/// Puede cambiar los equipos y los jugadores añadidos de un entrenamiento: el PRESIDENTE o quien
/// entrena alguna de sus categorías (spec 013, RF-016). Siempre falso en los partidos.
/// </param>
public record PermisosDeActividadDto(
    bool Gestiona, bool VePlanilla, bool Responde, bool IndicaResultado = false, bool Dirige = false);
