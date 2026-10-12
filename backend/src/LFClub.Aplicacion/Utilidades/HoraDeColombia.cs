using LFClub.Aplicacion.Interfaces;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa la fecha y la hora de pared de Colombia, en las que se programan y se comparan los
/// entrenamientos y los partidos (spec 009, research §2).
/// Su responsabilidad es convertir el instante del <see cref="IReloj"/> a la fecha y la hora de
/// Colombia con el desfase fijo de UTC−5. Colombia no tiene horario de verano desde 1993, así que
/// el desfase fijo es exacto; por eso no se usa <see cref="TimeZoneInfo"/>, que dependería de que la
/// imagen del contenedor traiga la base de zonas horarias para dar un resultado que ya se conoce.
/// No decide si una actividad ya empezó o terminó: eso lo hace <c>ReglaMomentoDeActividad</c>.
/// </summary>
public static class HoraDeColombia
{
    /// <summary>Desfase de la hora de Colombia respecto de UTC.</summary>
    public static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    /// <summary>Fecha y hora de Colombia del instante actual del reloj.</summary>
    public static (DateOnly Hoy, TimeOnly Hora) Ahora(IReloj reloj) => De(reloj.AhoraUtc);

    /// <summary>Fecha y hora de Colombia de un instante en UTC.</summary>
    public static (DateOnly Hoy, TimeOnly Hora) De(DateTime instanteUtc)
    {
        var local = DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc) + Desfase;
        return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
    }
}
