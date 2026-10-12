using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los ingresos de un club: su sala de espera, la aprobación y el
/// rechazo de un ingreso y la lista de ingresos aprobados.
/// Su responsabilidad es recibir la petición y delegar en los servicios.
/// No contiene reglas de negocio: solo admite al PRESIDENTE del club de la ruta, lo que comprueba
/// <see cref="IntegranteDelClubAttribute"/>; un DIRECTIVO, un ENTRENADOR o un JUGADOR reciben 403
/// en los cuatro endpoints (RF-017). No ofrece ninguna operación que elija o cambie el rol de
/// nadie ni que modifique la lista de aprobados.
/// </summary>
[Route("api/clubes/{clubId:guid}/ingresos")]
[Tags("Ingresos")]
[IntegranteDelClub(Rol.PRESIDENTE)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorIngresos : ControladorBase
{
    private readonly IServicioConsultaIngresos _consulta;
    private readonly IServicioAprobacionIngreso _aprobacion;
    private readonly IServicioRechazoIngreso _rechazo;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorIngresos(
        IServicioConsultaIngresos consulta, IServicioAprobacionIngreso aprobacion, IServicioRechazoIngreso rechazo)
    {
        _consulta = consulta;
        _aprobacion = aprobacion;
        _rechazo = rechazo;
    }

    /// <summary>Sala de espera del club: personas pendientes de aprobación.</summary>
    [HttpGet("en-espera")]
    [ProducesResponseType<IReadOnlyList<IngresoEnEsperaDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<IngresoEnEsperaDto>> ListarEnEspera(CancellationToken cancelacion) =>
        _consulta.ListarEnEsperaAsync(cancelacion);

    /// <summary>Ingresos aprobados del club, de solo lectura.</summary>
    [HttpGet("aprobados")]
    [ProducesResponseType<IReadOnlyList<IngresoAprobadoDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<IngresoAprobadoDto>> ListarAprobados(CancellationToken cancelacion) =>
        _consulta.ListarAprobadosAsync(cancelacion);

    /// <summary>
    /// Aprueba el ingreso de una persona en espera, que entra como JUGADOR. El cuerpo es opcional.
    /// </summary>
    [HttpPost("{usuarioRolId:guid}/aprobacion")]
    [ProducesResponseType<IngresoAprobadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<IngresoAprobadoDto> Aprobar(
        Guid usuarioRolId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AprobarIngresoDto? datos,
        CancellationToken cancelacion) =>
        _aprobacion.AprobarAsync(usuarioRolId, datos, Integrante, cancelacion);

    /// <summary>Rechaza el ingreso de una persona en espera y la borra del club.</summary>
    [HttpPost("{usuarioRolId:guid}/rechazo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Rechazar(Guid usuarioRolId, CancellationToken cancelacion)
    {
        await _rechazo.RechazarAsync(usuarioRolId, cancelacion);
        return NoContent();
    }
}
