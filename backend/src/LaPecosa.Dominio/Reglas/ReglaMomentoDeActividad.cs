namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de en qué momento está una actividad respecto de ahora (spec 009,
/// research §2): si ya empezó, si ya terminó y si su fecha ya llegó. Todo se compara en hora de
/// Colombia, la de pared en que se programó la actividad.
/// Su responsabilidad es decidirlo con la fecha y la hora de la actividad y la fecha y la hora de
/// ahora. Ya empezó cuando <c>Fecha + HoraInicio &lt;= ahora</c>: desde ese instante la familia ya
/// no responde y el resultado ya se puede registrar. Su fecha llegó cuando <c>Fecha &lt;= hoy</c>:
/// desde ese día se registra la asistencia.
/// No lee el reloj ni convierte zonas horarias: recibe la hora de Colombia ya calculada.
/// </summary>
public static class ReglaMomentoDeActividad
{
    /// <summary>Indica si la actividad ya empezó: su fecha y hora de inicio no son posteriores a ahora.</summary>
    public static bool YaEmpezo(DateOnly fecha, TimeOnly horaInicio, DateOnly hoy, TimeOnly ahora) =>
        NoEsPosterior(fecha, horaInicio, hoy, ahora);

    /// <summary>Indica si el entrenamiento ya terminó: su fecha y hora de fin no son posteriores a ahora.</summary>
    public static bool YaTermino(DateOnly fecha, TimeOnly horaFin, DateOnly hoy, TimeOnly ahora) =>
        NoEsPosterior(fecha, horaFin, hoy, ahora);

    /// <summary>Indica si la fecha de la actividad ya llegó: es hoy o un día anterior.</summary>
    public static bool SuFechaLlego(DateOnly fecha, DateOnly hoy) => fecha <= hoy;

    private static bool NoEsPosterior(DateOnly fecha, TimeOnly hora, DateOnly hoy, TimeOnly ahora) =>
        fecha < hoy || (fecha == hoy && hora <= ahora);
}
