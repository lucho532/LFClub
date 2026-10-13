using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa el conjunto de jugadores al que llega un aviso de actividad (spec 012, RF-007 a
/// RF-009; research §7).
/// Su responsabilidad es nombrar las cuatro coberturas que resuelve el repositorio con una consulta
/// del club.
/// No lista a los jugadores ni excluye a quien hizo la acción: eso es del repositorio.
/// </summary>
public enum CoberturaDeAviso
{
    /// <summary>
    /// Jugadores activos en las categorías de la actividad. En un partido ya jugado suma a los
    /// convocados, para que un añadido de otra categoría reciba el resultado (spec 013, research §8).
    /// </summary>
    CATEGORIAS,

    /// <summary>
    /// Jugadores activos de la audiencia de un entrenamiento: los de sus equipos, o los de sus
    /// categorías si no tiene equipos, más los añadidos (spec 013, RF-024). Sin equipos ni añadidos
    /// es exactamente <see cref="CATEGORIAS"/> (RF-025).
    /// </summary>
    AUDIENCIA_DE_ENTRENAMIENTO,

    /// <summary>Jugadores activos con fila en la convocatoria del partido.</summary>
    CONVOCADOS,

    /// <summary>Solo los jugadores a los que alude el aviso (los recién convocados).</summary>
    ALUDIDOS,
}

/// <summary>
/// Representa la regla de a quién llega cada hecho de actividad (constitución §17.2; spec 012,
/// RF-007 a RF-010; research §7).
/// Su responsabilidad es decidir la cobertura con el hecho y, en un partido, con su estado: los hechos
/// de un entrenamiento van a su audiencia (equipos y añadidos, spec 013); un
/// partido próximo solo se avisa a sus convocados porque solo ellos lo ven; uno ya jugado lo ve todo
/// el club (spec 009, RF-019) y se avisa a las categorías.
/// No consulta la base de datos ni decide si un jugador está activo.
/// </summary>
public static class ReglaDestinatariosDeAviso
{
    /// <summary>Decide la cobertura de un hecho.</summary>
    /// <param name="hecho">Lo que pasó con la actividad.</param>
    /// <param name="estadoDelPartido">Estado del partido al generar el aviso; nulo en los entrenamientos.</param>
    public static CoberturaDeAviso Evaluar(HechoDeActividad hecho, EstadoPartido? estadoDelPartido = null)
    {
        return hecho switch
        {
            HechoDeActividad.JUGADOR_CONVOCADO => CoberturaDeAviso.ALUDIDOS,
            HechoDeActividad.ENTRENAMIENTO_PROGRAMADO
                or HechoDeActividad.ENTRENAMIENTO_CAMBIADO
                or HechoDeActividad.ENTRENAMIENTO_CANCELADO
                or HechoDeActividad.ENTRENAMIENTO_REACTIVADO
                or HechoDeActividad.ASISTENCIA_REGISTRADA => CoberturaDeAviso.AUDIENCIA_DE_ENTRENAMIENTO,
            HechoDeActividad.PARTIDO_CAMBIADO
                or HechoDeActividad.PARTIDO_CANCELADO
                or HechoDeActividad.PARTIDO_REACTIVADO
                when estadoDelPartido != EstadoPartido.JUGADO => CoberturaDeAviso.CONVOCADOS,
            _ => CoberturaDeAviso.CATEGORIAS,
        };
    }
}
