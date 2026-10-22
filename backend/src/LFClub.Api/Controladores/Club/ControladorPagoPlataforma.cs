using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints con los que el PRESIDENTE ve el cobro de la plataforma a su club y lo paga
/// (spec 017, RF-007 a RF-011): el resumen, un cobro y el inicio de un intento de pago.
/// Su responsabilidad es recibir la petición y delegar en los servicios. Solo el PRESIDENTE entra,
/// también con el club suspendido; los demás roles reciben <c>403</c> y un cobro de otro club, <c>404</c>.
/// No contiene reglas de negocio y sus respuestas no se guardan en caché.
/// </summary>
[Route("api/clubes/{clubId:guid}/pago-plataforma")]
[Tags("PagoPlataforma")]
[IntegranteDelClub(Rol.PRESIDENTE)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorPagoPlataforma : ControladorBase
{
    private readonly IServicioConsultaPagoPlataforma _consulta;
    private readonly IServicioPagoPlataforma _pago;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorPagoPlataforma(IServicioConsultaPagoPlataforma consulta, IServicioPagoPlataforma pago)
    {
        _consulta = consulta;
        _pago = pago;
    }

    /// <summary>Cobros pendientes e historial del club.</summary>
    [HttpGet]
    [ProducesResponseType<PagoPlataformaDto>(StatusCodes.Status200OK)]
    public Task<PagoPlataformaDto> Obtener(Guid clubId, CancellationToken cancelacion)
    {
        SinCache();
        return _consulta.ObtenerAsync(clubId, cancelacion);
    }

    /// <summary>Un cobro con su detalle; la pantalla de retorno lo usa para ver si ya figura pagado.</summary>
    [HttpGet("cobros/{cobroId:guid}")]
    [ProducesResponseType<CobroPlataformaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<CobroPlataformaDto> ObtenerCobro(Guid clubId, Guid cobroId, CancellationToken cancelacion)
    {
        SinCache();
        return _consulta.ObtenerCobroAsync(clubId, cobroId, cancelacion);
    }

    /// <summary>Inicia un intento de pago y devuelve la dirección de la pasarela.</summary>
    [HttpPost("cobros/{cobroId:guid}/intentos")]
    [ProducesResponseType<IntentoDePagoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status503ServiceUnavailable, Problema.TipoContenido)]
    public async Task<IActionResult> IniciarPago(Guid clubId, Guid cobroId, CancellationToken cancelacion)
    {
        SinCache();
        return StatusCode(StatusCodes.Status201Created, await _pago.IniciarAsync(clubId, cobroId, UsuarioId, cancelacion));
    }

    private void SinCache() => Response.Headers.CacheControl = "private, no-store";
}
