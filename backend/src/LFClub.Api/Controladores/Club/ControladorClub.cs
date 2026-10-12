using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints del club elegido por uno de sus integrantes.
/// Su responsabilidad es recibir la petición y delegar en los servicios: los datos del club, su
/// configuración y el resumen del inicio del PRESIDENTE y de los DIRECTIVOS (spec 007).
/// No contiene reglas de negocio: la pertenencia, el estado del club y el rol mínimo los comprueba
/// <see cref="IntegranteDelClubAttribute"/> en cada petición.
/// </summary>
[Route("api/clubes/{clubId:guid}")]
[Tags("Club")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorClub : ControladorBase
{
    private readonly IServicioConsultaClub _consulta;
    private readonly IServicioConfiguracionClub _configuracion;
    private readonly IServicioResumenClub _resumen;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorClub(
        IServicioConsultaClub consulta, IServicioConfiguracionClub configuracion, IServicioResumenClub resumen)
    {
        _consulta = consulta;
        _configuracion = configuracion;
        _resumen = resumen;
    }

    /// <summary>Datos e identidad del club elegido.</summary>
    [HttpGet]
    [IntegranteDelClub]
    [ProducesResponseType<ClubDto>(StatusCodes.Status200OK)]
    public Task<ClubDto> Obtener(CancellationToken cancelacion) => _consulta.ObtenerAsync(Integrante, cancelacion);

    /// <summary>El PRESIDENTE edita nombre, sede, dirección y contacto de su club.</summary>
    [HttpPut("configuracion")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<ClubDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<ClubDto> ActualizarConfiguracion(ActualizarConfiguracionClubDto datos, CancellationToken cancelacion) =>
        _configuracion.ActualizarElPropioAsync(datos, Integrante, cancelacion);

    /// <summary>
    /// Las cuatro cifras del inicio: jugadores, equipos, entrenadores y categorías. Solo el
    /// PRESIDENTE y los DIRECTIVOS, también si entrenan una categoría.
    /// </summary>
    [HttpGet("resumen")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]
    [ProducesResponseType<ResumenClubDto>(StatusCodes.Status200OK)]
    public Task<ResumenClubDto> Resumen(CancellationToken cancelacion) => _resumen.ObtenerAsync(cancelacion);
}
