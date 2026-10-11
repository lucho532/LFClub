using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa el endpoint del calendario del club: los entrenamientos y los partidos que quien
/// pregunta puede ver en un rango de días (spec 009).
/// Su responsabilidad es recibir el rango y delegar en el servicio con el integrante de la
/// petición.
/// No contiene reglas de negocio ni decide quién ve qué.
/// </summary>
[Route("api/clubes/{clubId:guid}/calendario")]
[Tags("Calendario")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorCalendario : ControladorBase
{
    private readonly IServicioCalendario _calendario;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ControladorCalendario(IServicioCalendario calendario)
    {
        _calendario = calendario;
    }

    /// <summary>Lo que quien pregunta puede ver entre dos fechas, ambas incluidas (hasta 62 días).</summary>
    [HttpGet]
    [IntegranteDelClub]
    [ProducesResponseType<CalendarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public Task<CalendarioDto> Obtener([FromQuery] string? desde, [FromQuery] string? hasta, CancellationToken cancelacion) =>
        _calendario.ObtenerAsync(desde, hasta, Integrante, cancelacion);
}
