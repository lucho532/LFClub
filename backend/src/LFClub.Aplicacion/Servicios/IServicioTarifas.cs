using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta y el cambio de las tarifas del cobro de la plataforma (spec 017, RF-001 y
/// RF-002).
/// Su responsabilidad es leer y guardar los cuatro precios y los días de gracia, validándolos, y dejar
/// quién y cuándo los cambió. Los cambios solo afectan a los cobros que se generen después.
/// No toca cobros ya generados ni muestra llaves o secretos de la pasarela.
/// </summary>
public interface IServicioTarifas
{
    /// <summary>Las tarifas vigentes y si la pasarela está configurada.</summary>
    Task<TarifasDto> ObtenerAsync(CancellationToken cancelacion = default);

    /// <summary>Guarda las tarifas; precios nulos o enteros no negativos, y días de gracia de 0 a 30.</summary>
    Task<TarifasDto> GuardarAsync(GuardarTarifasDto datos, Guid usuarioId, CancellationToken cancelacion = default);
}
