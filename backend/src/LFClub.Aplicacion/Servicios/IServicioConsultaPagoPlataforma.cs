using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta que hace el PRESIDENTE del cobro de la plataforma a su club (spec 017,
/// RF-007 a RF-011).
/// Su responsabilidad es armar el resumen (cobros pendientes, total, historial y los indicadores de
/// pago en línea y de suspensión por impago) y un cobro con su detalle, siempre desde lo guardado al
/// generar el cobro.
/// No cambia nada y no resuelve un cobro de otro club: responde que no existe.
/// </summary>
public interface IServicioConsultaPagoPlataforma
{
    /// <summary>El resumen de pago del club.</summary>
    Task<PagoPlataformaDto> ObtenerAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>Un cobro del club; <c>404</c> si no existe o es de otro club.</summary>
    Task<CobroPlataformaDto> ObtenerCobroAsync(Guid clubId, Guid cobroId, CancellationToken cancelacion = default);
}
