using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Plataforma;

/// <summary>
/// Representa el endpoint del panel de administración para quitar el rol a un presidente.
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioRetiroPresidente"/>.
/// No contiene reglas de negocio; solo admite a la cuenta DESARROLLADOR.
/// </summary>
[Route("api/plataforma/clubes/{clubId:guid}/presidentes")]
[Tags("Plataforma")]
[SoloDesarrollador]
public class ControladorPresidentes : ControladorBase
{
    private readonly IServicioRetiroPresidente _servicio;

    /// <summary>Crea el controlador con el servicio de retiro.</summary>
    public ControladorPresidentes(IServicioRetiroPresidente servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Quita el rol a un presidente, asignándole otro rol o eliminándolo del club.</summary>
    [HttpPost("{usuarioRolId:guid}/retiro")]
    [ProducesResponseType<ClubDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<ClubDetalleDto> Retirar(
        Guid clubId, Guid usuarioRolId, RetirarPresidenteDto datos, CancellationToken cancelacion) =>
        _servicio.RetirarAsync(clubId, usuarioRolId, datos, cancelacion);
}
