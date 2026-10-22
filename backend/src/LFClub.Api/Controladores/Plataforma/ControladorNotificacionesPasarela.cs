using LFClub.Api.Errores;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Plataforma;

/// <summary>
/// Representa el endpoint al que la pasarela de pago envía sus notificaciones (spec 017, RF-010).
/// Su responsabilidad es leer el cuerpo tal como llegó y la cabecera <c>X-Event-Checksum</c> y
/// entregárselos a <see cref="IServicioNotificacionPasarela"/>: responde <c>200</c> a toda notificación
/// bien firmada, aunque no cambie nada, para que la pasarela no reintente sin fin, y <c>401</c> si la
/// firma no es válida. Es anónimo de forma explícita (§9): no hay sesión, la autenticidad es la firma.
/// No contiene reglas de negocio y no registra el cuerpo.
/// </summary>
[Route("api/plataforma/pagos/notificaciones")]
[Tags("PagoPlataforma")]
[AllowAnonymous]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
public class ControladorNotificacionesPasarela : ControladorBase
{
    private const int MaximoDeBytes = 64 * 1024;

    private readonly IServicioNotificacionPasarela _servicio;

    /// <summary>Crea el controlador con el servicio de notificaciones.</summary>
    public ControladorNotificacionesPasarela(IServicioNotificacionPasarela servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Recibe una notificación de la pasarela.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [RequestSizeLimit(MaximoDeBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Recibir(
        [FromHeader(Name = "X-Event-Checksum")] string? checksum, CancellationToken cancelacion)
    {
        using var lector = new StreamReader(Request.Body);
        var cuerpo = await lector.ReadToEndAsync(cancelacion);

        await _servicio.RecibirAsync(cuerpo, checksum, cancelacion);
        return Ok();
    }
}
