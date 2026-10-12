using LFClub.Api.Configuracion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LFClub.Api.Controladores.Cuenta;

/// <summary>
/// Representa los endpoints de quien recibe una invitación: consultarla, registrarse con ella o
/// aceptarla con una cuenta que ya existe.
/// Su responsabilidad es recibir la petición y delegar en los servicios. Quien se registra o
/// acepta entra directamente al club con el rol de su invitación, sin sala de espera.
/// No contiene reglas de negocio ni recibe un rol, un club o un correo: salen de la invitación. El
/// token llega en el cuerpo, nunca en la dirección.
/// </summary>
[Route("api/invitaciones")]
[Tags("Invitaciones")]
[ProducesResponseType<Problema>(StatusCodes.Status410Gone, Problema.TipoContenido)]
public class ControladorInvitaciones : ControladorBase
{
    private readonly IServicioRegistroConInvitacion _registro;
    private readonly IServicioAceptacionInvitacion _aceptacion;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorInvitaciones(
        IServicioRegistroConInvitacion registro, IServicioAceptacionInvitacion aceptacion)
    {
        _registro = registro;
        _aceptacion = aceptacion;
    }

    /// <summary>Devuelve los datos fijos de una invitación para mostrar el registro.</summary>
    [HttpPost("consulta")]
    [AllowAnonymous]
    [EnableRateLimiting(ConfiguracionSeguridad.PoliticaAnonimo)]
    [ProducesResponseType<InvitacionVigenteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status429TooManyRequests, Problema.TipoContenido)]
    public Task<InvitacionVigenteDto> Consultar(TokenDto datos, CancellationToken cancelacion) =>
        _registro.ConsultarAsync(datos, cancelacion);

    /// <summary>Registra una cuenta nueva con una invitación, la deja dentro del club e inicia su sesión.</summary>
    [HttpPost("registro")]
    [AllowAnonymous]
    [EnableRateLimiting(ConfiguracionSeguridad.PoliticaAnonimo)]
    [ProducesResponseType<TokenSesionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status429TooManyRequests, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Registrar(RegistrarConInvitacionDto datos, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await _registro.RegistrarAsync(datos, cancelacion));

    /// <summary>Añade a quien ya tiene cuenta al club de la invitación. Exige sesión.</summary>
    [HttpPost("aceptacion")]
    [ProducesResponseType<ClubDeSesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ClubDeSesionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Aceptar(AceptarInvitacionDto datos, CancellationToken cancelacion)
    {
        var (club, creada) = await _aceptacion.AceptarAsync(datos, UsuarioId, cancelacion);
        return StatusCode(creada ? StatusCodes.Status201Created : StatusCodes.Status200OK, club);
    }
}
