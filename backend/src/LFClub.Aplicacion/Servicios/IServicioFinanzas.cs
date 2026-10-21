using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta de las finanzas generales del club (spec 015, RF-031 a RF-034).
/// Su responsabilidad es entregar el recaudo de un mes, la cartera pendiente, los totales por categoría y
/// quiénes deben, calculados al pedirlos de los cargos y los pagos confirmados del club.
/// No guarda totales, no cuenta pagos en revisión, rechazados ni anulados, no comprueba el rol (lo hace la
/// autorización del controlador) y no restringe nada a quien deba (§16.8).
/// </summary>
public interface IServicioFinanzas
{
    /// <summary>
    /// Las finanzas del mes indicado (primer día del mes) o, sin indicarlo, del mes en curso en Colombia. Un
    /// día que no es el primero del mes: 400.
    /// </summary>
    Task<FinanzasDto> ObtenerAsync(DateOnly? mes, CancellationToken cancelacion = default);
}
