namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que informa la familia al avisar de una transferencia (spec 015, RF-021): los datos
/// del formulario, sin la imagen, que va aparte.
/// Su responsabilidad es llevar el valor, la entidad, la referencia y la fecha.
/// No valida nada: lo hace <c>ValidadorPago</c>. No elige cargos: solo el PRESIDENTE al confirmar.
/// </summary>
/// <param name="Valor">Valor en pesos enteros, mayor que cero.</param>
/// <param name="Entidad">Entidad desde la que se transfirió; obligatoria.</param>
/// <param name="Referencia">Número de comprobante, opcional.</param>
/// <param name="Fecha">Fecha en que se transfirió; no futura.</param>
public record InformarPagoDto(decimal Valor, string? Entidad, string? Referencia, DateOnly Fecha);
