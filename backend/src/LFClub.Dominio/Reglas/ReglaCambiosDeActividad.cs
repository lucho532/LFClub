namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa lo que cambió en un entrenamiento o en un partido entre dos estados (spec 012, RF-004
/// y RF-029).
/// Su responsabilidad es nombrar los tres datos que avisan a las familias. Se combinan: un cambio
/// puede ser a la vez de fecha y de lugar.
/// No incluye la nota del entrenamiento ni el rival, la condición o el torneo del partido.
/// </summary>
[Flags]
public enum CambiosDeActividad
{
    /// <summary>No cambió ninguno de los tres datos; no se avisa.</summary>
    Ninguno = 0,

    /// <summary>Cambió la fecha.</summary>
    Fecha = 1,

    /// <summary>Cambió la hora de inicio o, en un entrenamiento, la de fin.</summary>
    Hora = 2,

    /// <summary>Cambió el lugar.</summary>
    Lugar = 4,
}

/// <summary>
/// Representa la regla de cuándo un cambio en una actividad merece un aviso (spec 012, RF-004,
/// RF-026 y RF-029; research §6).
/// Su responsabilidad es comparar fecha, hora (inicio o fin) y lugar de antes y de después y decir
/// qué cambió. Guardar lo mismo no cambia nada; cambiar solo la nota de un entrenamiento, o el
/// rival, la condición o el torneo de un partido, tampoco es un cambio que avise.
/// No consulta la base de datos ni decide quién recibe el aviso.
/// </summary>
public static class ReglaCambiosDeActividad
{
    /// <summary>Compara el estado de una actividad antes y después de un cambio.</summary>
    /// <param name="fechaAntes">Fecha antes del cambio.</param>
    /// <param name="inicioAntes">Hora de inicio antes del cambio.</param>
    /// <param name="finAntes">Hora de fin antes del cambio; nula en un partido.</param>
    /// <param name="lugarAntes">Lugar antes del cambio.</param>
    /// <param name="fechaDespues">Fecha después del cambio.</param>
    /// <param name="inicioDespues">Hora de inicio después del cambio.</param>
    /// <param name="finDespues">Hora de fin después del cambio; nula en un partido.</param>
    /// <param name="lugarDespues">Lugar después del cambio.</param>
    public static CambiosDeActividad Evaluar(
        DateOnly fechaAntes,
        TimeOnly inicioAntes,
        TimeOnly? finAntes,
        string? lugarAntes,
        DateOnly fechaDespues,
        TimeOnly inicioDespues,
        TimeOnly? finDespues,
        string? lugarDespues)
    {
        var cambios = CambiosDeActividad.Ninguno;

        if (fechaAntes != fechaDespues)
        {
            cambios |= CambiosDeActividad.Fecha;
        }

        if (inicioAntes != inicioDespues || finAntes != finDespues)
        {
            cambios |= CambiosDeActividad.Hora;
        }

        if (!string.Equals(lugarAntes ?? string.Empty, lugarDespues ?? string.Empty, StringComparison.Ordinal))
        {
            cambios |= CambiosDeActividad.Lugar;
        }

        return cambios;
    }
}
