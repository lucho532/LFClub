namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la generación del cobro mensual de la plataforma (spec 017, RF-003 a RF-006).
/// Su responsabilidad es crear, sin duplicados, el cobro del mes en curso de cada club al que ya le
/// toca, con el detalle por rol y los precios vigentes congelados.
/// No cobra, no suspende y no crea cobros de meses pasados.
/// </summary>
public interface IServicioCobrosPlataforma
{
    /// <summary>Genera los cobros del mes en curso que falten. Devuelve cuántos creó.</summary>
    Task<int> GenerarAsync(CancellationToken cancelacion = default);
}
