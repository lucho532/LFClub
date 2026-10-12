using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de la planilla y la asistencia de un entrenamiento (spec 009,
/// historia 4).
/// Su responsabilidad es recibir la petición y delegar en el servicio con el integrante de la
/// petición.
/// No contiene reglas de negocio. La cuenta de un jugador nunca recibe la planilla: la autorización
/// la niega con <c>403 rol_no_autorizado</c> (RF-024).
/// </summary>
[Route("api/clubes/{clubId:guid}/entrenamientos/{entrenamientoId:guid}")]
[Tags("Entrenamientos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorAsistencia : ControladorBase
{
    private readonly IServicioAsistencia _asistencia;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ControladorAsistencia(IServicioAsistencia asistencia)
    {
        _asistencia = asistencia;
    }

    /// <summary>La planilla con respuestas y asistencia de los jugadores que quien pregunta puede marcar.</summary>
    [HttpGet("jugadores")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PlanillaEntrenamientoDto>(StatusCodes.Status200OK)]
    public Task<PlanillaEntrenamientoDto> Planilla(Guid entrenamientoId, CancellationToken cancelacion) =>
        _asistencia.PlanillaAsync(entrenamientoId, Integrante, cancelacion);

    /// <summary>Registra o corrige la asistencia de los jugadores enviados.</summary>
    [HttpPut("asistencia")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PlanillaEntrenamientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PlanillaEntrenamientoDto> Registrar(Guid entrenamientoId, RegistrarAsistenciaDto datos, CancellationToken cancelacion) =>
        _asistencia.RegistrarAsync(entrenamientoId, datos, Integrante, cancelacion);
}
