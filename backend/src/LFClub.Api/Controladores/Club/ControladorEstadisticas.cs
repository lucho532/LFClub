using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de las estadísticas deportivas: las de un jugador y los goleadores del
/// club (spec 009, historia 6).
/// Su responsabilidad es recibir la petición y delegar en el servicio con el integrante de la
/// petición.
/// No contiene reglas de negocio ni calcula nada.
/// </summary>
[Route("api/clubes/{clubId:guid}")]
[Tags("Estadisticas")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorEstadisticas : ControladorBase
{
    private readonly IServicioEstadisticas _estadisticas;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ControladorEstadisticas(IServicioEstadisticas estadisticas)
    {
        _estadisticas = estadisticas;
    }

    /// <summary>Las estadísticas de un jugador, en total y por categoría.</summary>
    [HttpGet("jugadores/{usuarioRolId:guid}/estadisticas")]
    [IntegranteDelClub]
    [ProducesResponseType<EstadisticasJugadorDto>(StatusCodes.Status200OK)]
    public Task<EstadisticasJugadorDto> DelJugador(Guid usuarioRolId, CancellationToken cancelacion) =>
        _estadisticas.DelJugadorAsync(usuarioRolId, Integrante, cancelacion);

    /// <summary>Las categorías con goles y, con <c>categoriaId</c>, sus goleadores.</summary>
    [HttpGet("goleadores")]
    [IntegranteDelClub]
    [ProducesResponseType<GoleadoresDto>(StatusCodes.Status200OK)]
    public Task<GoleadoresDto> Goleadores([FromQuery] Guid? categoriaId, CancellationToken cancelacion) =>
        _estadisticas.GoleadoresAsync(categoriaId, Integrante, cancelacion);
}
