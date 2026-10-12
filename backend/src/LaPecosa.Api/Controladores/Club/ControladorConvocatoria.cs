using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de la planilla y la convocatoria de un partido (spec 009, historia 2).
/// Su responsabilidad es recibir la petición y delegar en el servicio con el integrante de la
/// petición.
/// No contiene reglas de negocio. La cuenta de un jugador nunca recibe la planilla: la autorización
/// la niega con <c>403 rol_no_autorizado</c> (RF-012).
/// </summary>
[Route("api/clubes/{clubId:guid}/partidos/{partidoId:guid}")]
[Tags("Partidos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorConvocatoria : ControladorBase
{
    private readonly IServicioConvocatoria _convocatoria;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ControladorConvocatoria(IServicioConvocatoria convocatoria)
    {
        _convocatoria = convocatoria;
    }

    /// <summary>La planilla del partido: convocados, respuestas, quiénes jugaron y convocables.</summary>
    [HttpGet("jugadores")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PlanillaPartidoDto>(StatusCodes.Status200OK)]
    public Task<PlanillaPartidoDto> Planilla(Guid partidoId, CancellationToken cancelacion) =>
        _convocatoria.PlanillaAsync(partidoId, Integrante, cancelacion);

    /// <summary>Deja la convocatoria exactamente con esos jugadores.</summary>
    [HttpPut("convocatoria")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PlanillaPartidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PlanillaPartidoDto> Guardar(Guid partidoId, GuardarConvocatoriaDto datos, CancellationToken cancelacion) =>
        _convocatoria.GuardarAsync(partidoId, datos, Integrante, cancelacion);
}
