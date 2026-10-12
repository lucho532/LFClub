namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa de qué clase es la actividad a la que se refiere un aviso de la campana (spec 012,
/// RF-001 a RF-003).
/// Su responsabilidad es distinguir un entrenamiento de un partido para saber a qué tabla apunta
/// <c>ActividadId</c> y a qué pantalla navega el aviso.
/// No decide si la familia puede ver la actividad: eso es de la consulta de disponibilidad.
/// </summary>
public enum TipoDeActividad
{
    /// <summary>La actividad es un entrenamiento del club.</summary>
    ENTRENAMIENTO,

    /// <summary>La actividad es un partido del club.</summary>
    PARTIDO,
}
