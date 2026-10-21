using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un cargo de un jugador con lo que ya se le pagó (spec 015, RF-001 y RF-024).
/// Su responsabilidad es llevar el tipo, el mes, el valor cobrado, lo aplicado por pagos confirmados
/// y lo que falta.
/// No lleva la categoría con la que se generó.
/// </summary>
/// <param name="Id">Identificador del cargo.</param>
/// <param name="Tipo">Mensualidad u otro.</param>
/// <param name="Mes">Primer día del mes que cobra; nulo si no es una mensualidad.</param>
/// <param name="Valor">Valor cobrado.</param>
/// <param name="Pagado">Suma de lo aplicado por pagos confirmados.</param>
/// <param name="Pendiente">Lo que falta por pagar.</param>
public record CargoDto(Guid Id, TipoCargo Tipo, DateOnly? Mes, decimal Valor, decimal Pagado, decimal Pendiente);
