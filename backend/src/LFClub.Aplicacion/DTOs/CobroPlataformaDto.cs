using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un cobro mensual de la plataforma a un club (spec 017, RF-004 y RF-007).
/// Su responsabilidad es llevar el periodo, el estado, el detalle por rol, el total, la fecha desde
/// la que se suspende y, si ya se pagó o se anuló, el comprobante o el motivo.
/// No lleva llaves, secretos ni datos personales.
/// </summary>
/// <param name="CobroId">Identificador del cobro.</param>
/// <param name="Periodo">Mes que cobra, <c>AAAA-MM</c>.</param>
/// <param name="Estado">Estado del cobro.</param>
/// <param name="Detalle">Una línea por rol; vacío en los cobros por rango, que llevan <paramref name="Rango"/>.</param>
/// <param name="Total">Total a pagar, en pesos.</param>
/// <param name="GeneradoEn">Cuándo se generó, en UTC.</param>
/// <param name="SuspendeDesde">Primer día en que el club se suspende; el último día para pagar es el anterior.</param>
/// <param name="PagadoEn">Cuándo se pagó, en UTC; nulo si no está pagado.</param>
/// <param name="OrigenDelPago">De dónde vino el pago; nulo si no está pagado.</param>
/// <param name="Referencia">Referencia del pago por la pasarela, si la hay.</param>
/// <param name="MedioDePago">Medio de pago informado por la pasarela, si lo hay.</param>
/// <param name="EnVerificacion">Hay un intento de pago reciente sin respuesta de la pasarela; solo informativo.</param>
/// <param name="UltimoPagoNoAprobado">La pasarela informó que el último intento de pago no fue aprobado; se puede intentar de nuevo.</param>
/// <param name="MotivoAnulacion">Por qué se anuló; nulo si no está anulado.</param>
/// <param name="Jugadores">Jugadores activos del club al generar el cobro.</param>
/// <param name="Rango">Rango de la tarifa que se aplicó al generar el cobro; nulo en los cobros por precio de rol anteriores.</param>
public record CobroPlataformaDto(
    Guid CobroId,
    string Periodo,
    EstadoCobroPlataforma Estado,
    IReadOnlyList<DetalleDeRolDto> Detalle,
    decimal Total,
    DateTime GeneradoEn,
    DateOnly SuspendeDesde,
    DateTime? PagadoEn,
    OrigenPagoPlataforma? OrigenDelPago,
    string? Referencia,
    string? MedioDePago,
    bool EnVerificacion,
    bool UltimoPagoNoAprobado,
    string? MotivoAnulacion,
    RangoTarifaDto? Rango = null,
    int Jugadores = 0);
