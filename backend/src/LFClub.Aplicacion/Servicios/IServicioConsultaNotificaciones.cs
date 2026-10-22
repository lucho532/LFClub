using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta de las notificaciones recibidas de la pasarela (spec 017, RF-015).
/// Su responsabilidad es listar las últimas 100, la más reciente primero y filtrables por resultado,
/// para que el DESARROLLADOR revise duplicados y casos raros.
/// No devuelve el cuerpo ni la firma de las notificaciones.
/// </summary>
public interface IServicioConsultaNotificaciones
{
    /// <summary>Hasta 100 notificaciones, con el club y el periodo del cobro cuando existe.</summary>
    Task<IReadOnlyList<NotificacionPasarelaDto>> ListarAsync(
        ResultadoNotificacion? resultado, CancellationToken cancelacion = default);
}
