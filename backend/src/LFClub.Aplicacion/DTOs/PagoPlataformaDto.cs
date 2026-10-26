using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que ve el PRESIDENTE en «Pago de la plataforma» (spec 017, RF-007).
/// Su responsabilidad es llevar los cobros pendientes, su total, el historial, el último pago, el próximo cobro estimado y dos indicadores: si
/// el pago en línea está disponible y si el club está suspendido por impago.
/// No lleva llaves ni secretos de la pasarela.
/// </summary>
/// <param name="PagoEnLineaDisponible">Falso si la pasarela no está configurada.</param>
/// <param name="SuspendidoPorImpago">Verdadero si el club está suspendido por falta de pago.</param>
/// <param name="Pendientes">Cobros pendientes, el más antiguo primero.</param>
/// <param name="TotalPendiente">Suma de los cobros pendientes.</param>
/// <param name="Historial">Cobros pagados y anulados, el más reciente primero.</param>
/// <param name="UltimoPago">El último cobro que se pagó, con su comprobante; nulo si nunca ha pagado.</param>
/// <param name="EstadoDePago">Situación de pago del club, calculada en el servidor (al día, pendiente, vencido o suspendido por impago).</param>
/// <param name="ProximoCobro">Estimación del siguiente cobro; nulo si al club no se le cobra o no le corresponde cobro.</param>
public record PagoPlataformaDto(
    bool PagoEnLineaDisponible,
    bool SuspendidoPorImpago,
    IReadOnlyList<CobroPlataformaDto> Pendientes,
    decimal TotalPendiente,
    IReadOnlyList<CobroPlataformaDto> Historial,
    CobroPlataformaDto? UltimoPago = null,
    ProximoCobroDto? ProximoCobro = null,
    EstadoDePagoClub EstadoDePago = EstadoDePagoClub.SIN_COBRO);
