using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de las invitaciones que envía un club: ver las enviadas, invitar
/// indicando el rol, reenviar y cancelar.
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioInvitacionesClub"/>.
/// No contiene reglas de negocio: solo admite al PRESIDENTE del club de la ruta, lo que comprueba
/// <see cref="IntegranteDelClubAttribute"/>; un DIRECTIVO, un ENTRENADOR o un JUGADOR reciben 403
/// en los cuatro endpoints (RF-002 y RF-003). Nunca devuelve el token. No es el controlador del
/// mismo nombre del panel de la plataforma, que solo trata la invitación del presidente.
/// </summary>
[Route("api/clubes/{clubId:guid}/invitaciones")]
[Tags("Ingresos")]
[IntegranteDelClub(Rol.PRESIDENTE)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorInvitacionesClub : ControladorBase
{
    private readonly IServicioInvitacionesClub _servicio;

    /// <summary>Crea el controlador con el servicio de invitaciones del club.</summary>
    public ControladorInvitacionesClub(IServicioInvitacionesClub servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Invitaciones enviadas desde el club, una por correo (la más reciente).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InvitacionClubDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<InvitacionClubDto>> Listar(CancellationToken cancelacion) =>
        _servicio.ListarAsync(cancelacion);

    /// <summary>Invita a una persona por correo a registrarse en el club con un rol.</summary>
    [HttpPost]
    [ProducesResponseType<InvitacionClubDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Invitar(InvitarAlClubDto datos, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.InvitarAsync(datos, UsuarioId, cancelacion));

    /// <summary>
    /// Reenvía una invitación pendiente al mismo correo y con el mismo rol; el enlace anterior deja
    /// de servir.
    /// </summary>
    [HttpPost("{invitacionId:guid}/reenvio")]
    [ProducesResponseType<InvitacionClubDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Reenviar(Guid invitacionId, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.ReenviarAsync(invitacionId, UsuarioId, cancelacion));

    /// <summary>Cancela una invitación pendiente.</summary>
    [HttpPost("{invitacionId:guid}/cancelacion")]
    [ProducesResponseType<InvitacionClubDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<InvitacionClubDto> Cancelar(Guid invitacionId, CancellationToken cancelacion) =>
        _servicio.CancelarAsync(invitacionId, cancelacion);
}
