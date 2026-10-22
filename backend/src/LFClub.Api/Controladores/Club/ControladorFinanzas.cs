using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa el endpoint de las finanzas generales del club (spec 015, RF-031 a RF-034).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioFinanzas"/>. Lo consultan el
/// PRESIDENTE y el DIRECTIVO; el ENTRENADOR y la cuenta de un jugador no ven nada de esto:
/// <see cref="IntegranteDelClubAttribute"/> les responde <c>403</c>. No hay ninguna operación que cambie
/// nada: las cifras se calculan al pedirlas.
/// No contiene reglas de negocio.
/// </summary>
[Route("api/clubes/{clubId:guid}/finanzas")]
[Tags("Finanzas")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorFinanzas : ControladorBase
{
    private readonly IServicioFinanzas _servicio;

    /// <summary>Crea el controlador con el servicio de finanzas.</summary>
    public ControladorFinanzas(IServicioFinanzas servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Recaudo del mes, cartera pendiente, totales por categoría y quiénes deben.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]
    [ProducesResponseType<FinanzasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public Task<FinanzasDto> Obtener([FromQuery] DateOnly? mes, CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return _servicio.ObtenerAsync(mes, cancelacion);
    }
}
