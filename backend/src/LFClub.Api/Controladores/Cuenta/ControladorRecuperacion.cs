using LFClub.Api.Configuracion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LFClub.Api.Controladores.Cuenta;

/// <summary>
/// Representa los endpoints de recuperación de contraseña.
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioRecuperacion"/>.
/// No contiene reglas de negocio; son anónimos y con límite de peticiones por IP.
/// </summary>
[Route("api/cuenta/recuperacion")]
[Tags("Cuenta")]
[AllowAnonymous]
[EnableRateLimiting(ConfiguracionSeguridad.PoliticaAnonimo)]
[ProducesResponseType<Problema>(StatusCodes.Status429TooManyRequests, Problema.TipoContenido)]
public class ControladorRecuperacion : ControladorBase
{
    private readonly IServicioRecuperacion _servicio;

    /// <summary>Crea el controlador con el servicio de recuperación.</summary>
    public ControladorRecuperacion(IServicioRecuperacion servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Pide el correo de recuperación. Siempre responde lo mismo, exista o no el correo.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Pedir(PedirRecuperacionDto datos, CancellationToken cancelacion)
    {
        await _servicio.PedirAsync(datos, cancelacion);
        return Accepted();
    }

    /// <summary>Crea una contraseña nueva con el enlace recibido y desbloquea la cuenta.</summary>
    [HttpPost("confirmacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status410Gone, Problema.TipoContenido)]
    public async Task<IActionResult> Confirmar(RestablecerContrasenaDto datos, CancellationToken cancelacion)
    {
        await _servicio.ConfirmarAsync(datos, cancelacion);
        return NoContent();
    }
}
