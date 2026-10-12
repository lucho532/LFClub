namespace LaPecosa.Dominio.Enumeraciones;

/// <summary>
/// Representa lo que pasó con una actividad y generó un aviso en la campana (spec 012, RF-001 a
/// RF-003 y RF-010 a RF-014).
/// Su responsabilidad es nombrar los diez hechos que avisan: cinco de entrenamientos y cinco de
/// partidos. Crear un partido, corregir su rival y los demás cambios no están en la lista porque no
/// avisan (RF-003 y RF-029).
/// No guarda el texto ni decide los destinatarios: eso es de la redacción y de la regla de
/// destinatarios.
/// </summary>
public enum HechoDeActividad
{
    /// <summary>Se programó un entrenamiento.</summary>
    ENTRENAMIENTO_PROGRAMADO,

    /// <summary>Cambió la fecha, la hora o el lugar de un entrenamiento.</summary>
    ENTRENAMIENTO_CAMBIADO,

    /// <summary>Se canceló un entrenamiento.</summary>
    ENTRENAMIENTO_CANCELADO,

    /// <summary>Se reactivó un entrenamiento cancelado.</summary>
    ENTRENAMIENTO_REACTIVADO,

    /// <summary>Se registró la asistencia de un entrenamiento.</summary>
    ASISTENCIA_REGISTRADA,

    /// <summary>Cambió la fecha, la hora o el lugar de un partido.</summary>
    PARTIDO_CAMBIADO,

    /// <summary>Se canceló un partido.</summary>
    PARTIDO_CANCELADO,

    /// <summary>Se reactivó un partido cancelado.</summary>
    PARTIDO_REACTIVADO,

    /// <summary>Se convocó a uno o más jugadores a un partido.</summary>
    JUGADOR_CONVOCADO,

    /// <summary>Se registró o corrigió el resultado de un partido.</summary>
    RESULTADO_PUBLICADO,
}
