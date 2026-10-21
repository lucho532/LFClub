namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la generación de la mensualidad de cada mes (spec 015, RF-005 a RF-008).
/// Su responsabilidad es crear, sin intervención y sin duplicados, un cargo MENSUALIDAD del mes en
/// curso por cada jugador activo, con el valor vigente en ese momento.
/// No cobra, no avisa a nadie y no crea cargos de meses pasados.
/// </summary>
public interface IServicioGeneracionCargos
{
    /// <summary>Genera los cargos del mes en curso de todos los clubes. Devuelve cuántos creó.</summary>
    Task<int> GenerarAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Genera los cargos del mes en curso de un solo club, por ejemplo cuando el PRESIDENTE fija por
    /// primera vez el valor de la mensualidad. Devuelve cuántos creó.
    /// </summary>
    Task<int> GenerarDelClubAsync(Guid clubId, CancellationToken cancelacion = default);
}
