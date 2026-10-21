using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints del valor de la mensualidad: consultarlo, fijar el del club y fijar o
/// quitar el propio de uno o varios jugadores (spec 015, RF-011 y RF-012).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioMensualidad"/>. Consultan
/// el PRESIDENTE y el DIRECTIVO; solo el PRESIDENTE cambia. El ENTRENADOR y la cuenta de un jugador no
/// pueden hacer nada de esto: <see cref="IntegranteDelClubAttribute"/> les responde <c>403</c>.
/// No contiene reglas de negocio.
/// </summary>
[Route("api/clubes/{clubId:guid}/mensualidad")]
[Tags("Mensualidad")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorMensualidad : ControladorBase
{
    private readonly IServicioMensualidad _servicio;

    /// <summary>Crea el controlador con el servicio de la mensualidad.</summary>
    public ControladorMensualidad(IServicioMensualidad servicio)
    {
        _servicio = servicio;
    }

    /// <summary>El valor de la mensualidad del club y los jugadores con valor propio.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]
    [ProducesResponseType<MensualidadDto>(StatusCodes.Status200OK)]
    public Task<MensualidadDto> Obtener(CancellationToken cancelacion) => _servicio.ObtenerAsync(cancelacion);

    /// <summary>Fija el valor de la mensualidad del club.</summary>
    [HttpPut]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<MensualidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public Task<MensualidadDto> FijarValor(FijarValorMensualidadDto datos, CancellationToken cancelacion) =>
        _servicio.FijarValorDelClubAsync(datos, cancelacion);

    /// <summary>Fija o quita el valor propio de uno o varios jugadores.</summary>
    [HttpPut("jugadores")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<MensualidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<MensualidadDto> FijarValorPropio(FijarValorPropioDto datos, CancellationToken cancelacion) =>
        _servicio.FijarValorPropioAsync(datos, cancelacion);
}
