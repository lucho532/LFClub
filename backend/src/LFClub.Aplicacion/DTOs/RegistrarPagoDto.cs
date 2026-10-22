using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el PRESIDENTE para registrar un pago recibido directamente (spec 015,
/// RF-020).
/// Su responsabilidad es llevar el valor, el medio, la entidad, la referencia, la fecha y,
/// opcionalmente, los cargos que cubre.
/// No valida nada: lo hacen <c>ValidadorPago</c> y la regla de aplicación.
/// </summary>
/// <param name="Valor">Valor en pesos enteros, mayor que cero.</param>
/// <param name="Medio">Efectivo o transferencia.</param>
/// <param name="Entidad">Obligatoria si el medio es transferencia.</param>
/// <param name="Referencia">Número de comprobante, opcional.</param>
/// <param name="Fecha">Fecha en que se pagó; no futura.</param>
/// <param name="CargoIds">Cargos que cubre; sin ellos cubre los más antiguos primero.</param>
public record RegistrarPagoDto(
    decimal Valor, MedioPago Medio, string? Entidad, string? Referencia, DateOnly Fecha, IReadOnlyList<Guid>? CargoIds);
