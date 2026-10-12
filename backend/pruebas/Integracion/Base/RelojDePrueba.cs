using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Reloj de la API en las pruebas: el real mientras nadie lo fije, y fijo en el instante que diga
/// la prueba cuando necesita estar antes o después de que empiece una actividad. Lo comparten
/// todas las pruebas de la colección, que corren una tras otra: quien lo fija lo devuelve al real
/// al terminar (<see cref="UsarElReal"/>).
/// Las sesiones se emiten con este reloj y se validan con el real: conviene abrirlas antes de
/// fijarlo.
/// </summary>
public class RelojDePrueba : IReloj
{
    private DateTime? _fijo;

    public DateTime AhoraUtc => _fijo ?? DateTime.UtcNow;

    /// <summary>Hoy, en hora de Colombia, según este reloj.</summary>
    public DateOnly Hoy => HoraDeColombia.Ahora(this).Hoy;

    /// <summary>Fija el reloj en ese instante de Colombia.</summary>
    public void FijarEnColombia(DateOnly fecha, TimeOnly hora) =>
        _fijo = DateTime.SpecifyKind(fecha.ToDateTime(hora) - HoraDeColombia.Desfase, DateTimeKind.Utc);

    /// <summary>Vuelve al reloj real.</summary>
    public void UsarElReal() => _fijo = null;
}
