using LaPecosa.Api.Configuracion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LaPecosa.Api.Controladores.Cuenta;

/// <summary>
/// Representa los endpoints de la sesión: iniciar, consultar y renovar.
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioSesion"/>.
/// No contiene reglas de negocio ni compara contraseñas.
/// </summary>
[Route("api/sesion")]
[Tags("Sesion")]
public class ControladorSesion : ControladorBase
{
    private readonly IServicioSesion _servicio;

    /// <summary>Crea el controlador con el servicio de sesión.</summary>
    public ControladorSesion(IServicioSesion servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Inicia sesión con correo o documento y contraseña.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(ConfiguracionSeguridad.PoliticaAnonimo)]
    [ProducesResponseType<TokenSesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status429TooManyRequests, Problema.TipoContenido)]
    public Task<TokenSesionDto> Iniciar(IniciarSesionDto datos, CancellationToken cancelacion) =>
        _servicio.IniciarAsync(datos, cancelacion);

    /// <summary>
    /// Devuelve la cuenta actual y los clubes a los que pertenece, con los jugadores entre los que
    /// puede elegir en cada uno.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<SesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
    public Task<SesionDto> Obtener(CancellationToken cancelacion) =>
        _servicio.ObtenerAsync(UsuarioId, Limitacion, cancelacion);

    /// <summary>Cambia un token válido por uno nuevo, con la misma limitación.</summary>
    [HttpPost("renovacion")]
    [ProducesResponseType<TokenSesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
    public Task<TokenSesionDto> Renovar(CancellationToken cancelacion) =>
        _servicio.RenovarAsync(UsuarioId, Limitacion, cancelacion);
}
