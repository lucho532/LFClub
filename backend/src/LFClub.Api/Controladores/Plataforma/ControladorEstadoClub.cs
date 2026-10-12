using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Plataforma;

/// <summary>
/// Representa los endpoints del panel de administración para el estado de un club: suspender,
/// levantar la suspensión, dar de baja, revertir la baja y eliminar.
/// Su responsabilidad es recibir la petición y delegar en los servicios.
/// No contiene reglas de negocio; solo admite a la cuenta DESARROLLADOR.
/// </summary>
[Route("api/plataforma/clubes/{clubId:guid}")]
[Tags("Plataforma")]
[SoloDesarrollador]
[ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
public class ControladorEstadoClub : ControladorBase
{
    private readonly IServicioEstadoClub _estado;
    private readonly IServicioEliminacionClub _eliminacion;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorEstadoClub(IServicioEstadoClub estado, IServicioEliminacionClub eliminacion)
    {
        _estado = estado;
        _eliminacion = eliminacion;
    }

    /// <summary>Suspende, levanta la suspensión, da de baja o revierte la baja.</summary>
    [HttpPut("estado")]
    [ProducesResponseType<ClubDetalleDto>(StatusCodes.Status200OK)]
    public Task<ClubDetalleDto> Cambiar(Guid clubId, CambiarEstadoClubDto datos, CancellationToken cancelacion) =>
        _estado.CambiarAsync(clubId, datos, UsuarioId, cancelacion);

    /// <summary>Elimina de forma irreversible un club dado de baja.</summary>
    [HttpPost("eliminacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(Guid clubId, EliminarClubDto datos, CancellationToken cancelacion)
    {
        await _eliminacion.EliminarAsync(clubId, datos, cancelacion);
        return NoContent();
    }
}
