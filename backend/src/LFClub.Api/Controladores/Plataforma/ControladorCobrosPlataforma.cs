using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Plataforma;

/// <summary>
/// Representa los endpoints del panel de administración sobre los cobros de la plataforma (spec 017,
/// RF-021 a RF-023): el historial de un club, el pago manual, la anulación y las notificaciones de la
/// pasarela.
/// Su responsabilidad es recibir la petición y delegar en los servicios.
/// No contiene reglas de negocio; solo admite a la cuenta DESARROLLADOR.
/// </summary>
[Route("api/plataforma")]
[Tags("Plataforma")]
[SoloDesarrollador]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorCobrosPlataforma : ControladorBase
{
    private readonly IServicioCobrosDeUnClub _cobros;
    private readonly IServicioConsultaNotificaciones _notificaciones;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorCobrosPlataforma(IServicioCobrosDeUnClub cobros, IServicioConsultaNotificaciones notificaciones)
    {
        _cobros = cobros;
        _notificaciones = notificaciones;
    }

    /// <summary>Historial de cobros de un club, sin datos personales.</summary>
    [HttpGet("clubes/{clubId:guid}/cobros")]
    [ProducesResponseType<IReadOnlyList<CobroPlataformaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<IReadOnlyList<CobroPlataformaDto>> Listar(Guid clubId, CancellationToken cancelacion) =>
        _cobros.ListarAsync(clubId, cancelacion);

    /// <summary>Marca un cobro como pagado por fuera de la pasarela.</summary>
    [HttpPost("clubes/{clubId:guid}/cobros/{cobroId:guid}/pago-manual")]
    [ProducesResponseType<CobroPlataformaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CobroPlataformaDto> MarcarPagado(Guid clubId, Guid cobroId, CancellationToken cancelacion) =>
        _cobros.MarcarPagadoAsync(clubId, cobroId, UsuarioId, cancelacion);

    /// <summary>Anula un cobro pendiente, con motivo.</summary>
    [HttpPost("clubes/{clubId:guid}/cobros/{cobroId:guid}/anulacion")]
    [ProducesResponseType<CobroPlataformaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CobroPlataformaDto> Anular(
        Guid clubId, Guid cobroId, AnularCobroDto datos, CancellationToken cancelacion) =>
        _cobros.AnularAsync(clubId, cobroId, UsuarioId, datos, cancelacion);

    /// <summary>Últimas notificaciones de la pasarela, para revisar duplicados y casos raros.</summary>
    [HttpGet("notificaciones")]
    [ProducesResponseType<IReadOnlyList<NotificacionPasarelaDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<NotificacionPasarelaDto>> ListarNotificaciones(
        [FromQuery] ResultadoNotificacion? resultado, CancellationToken cancelacion) =>
        _notificaciones.ListarAsync(resultado, cancelacion);
}
