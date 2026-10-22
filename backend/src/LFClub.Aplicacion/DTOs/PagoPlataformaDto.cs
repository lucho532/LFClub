namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que ve el PRESIDENTE en «Pago de la plataforma» (spec 017, RF-007).
/// Su responsabilidad es llevar los cobros pendientes, su total, el historial y dos indicadores: si
/// el pago en línea está disponible y si el club está suspendido por impago.
/// No lleva llaves ni secretos de la pasarela.
/// </summary>
/// <param name="PagoEnLineaDisponible">Falso si la pasarela no está configurada.</param>
/// <param name="SuspendidoPorImpago">Verdadero si el club está suspendido por falta de pago.</param>
/// <param name="Pendientes">Cobros pendientes, el más antiguo primero.</param>
/// <param name="TotalPendiente">Suma de los cobros pendientes.</param>
/// <param name="Historial">Cobros pagados y anulados, el más reciente primero.</param>
public record PagoPlataformaDto(
    bool PagoEnLineaDisponible,
    bool SuspendidoPorImpago,
    IReadOnlyList<CobroPlataformaDto> Pendientes,
    decimal TotalPendiente,
    IReadOnlyList<CobroPlataformaDto> Historial);
