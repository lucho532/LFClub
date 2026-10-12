namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el reloj de la aplicación.
/// Su responsabilidad es dar la fecha y hora actuales, para poder sustituirlas en las pruebas.
/// No calcula vencimientos: eso lo hace cada servicio.
/// </summary>
public interface IReloj
{
    /// <summary>Fecha y hora actuales en UTC.</summary>
    DateTime AhoraUtc { get; }
}
