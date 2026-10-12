namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado actual de un entrenamiento (constitución §18; spec 009, RF-004 y RF-038).
/// Su responsabilidad es distinguir el entrenamiento que sigue en pie del que se canceló.
/// Un entrenamiento nace programado, se puede cancelar y, mientras no haya empezado, reactivar.
/// No guarda historial de estados ni decide quién puede cambiarlo.
/// </summary>
public enum EstadoEntrenamiento
{
    /// <summary>Sigue en pie: admite cambios, respuestas de las familias y asistencia.</summary>
    PROGRAMADO,

    /// <summary>Cancelado: sigue a la vista, marcado, y no admite cambios, respuestas ni asistencia.</summary>
    CANCELADO,
}
