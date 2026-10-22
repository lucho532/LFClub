using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Plataforma;

/// <summary>
/// Representa los endpoints de las tarifas del cobro de la plataforma y del mes desde el que se cobra
/// a cada club (spec 017, RF-001, RF-002 y RF-005a).
/// Su responsabilidad es recibir la petición y delegar en los servicios.
/// No contiene reglas de negocio; solo admite a la cuenta DESARROLLADOR.
/// </summary>
[Route("api/plataforma")]
[Tags("Plataforma")]
[SoloDesarrollador]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorTarifas : ControladorBase
{
    private readonly IServicioTarifas _tarifas;
    private readonly IServicioInicioDelCobro _inicioDelCobro;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorTarifas(IServicioTarifas tarifas, IServicioInicioDelCobro inicioDelCobro)
    {
        _tarifas = tarifas;
        _inicioDelCobro = inicioDelCobro;
    }

    /// <summary>Precios por rol y días de gracia vigentes.</summary>
    [HttpGet("tarifas")]
    [ProducesResponseType<TarifasDto>(StatusCodes.Status200OK)]
    public Task<TarifasDto> Obtener(CancellationToken cancelacion) => _tarifas.ObtenerAsync(cancelacion);

    /// <summary>Fija los precios y los días de gracia; aplican a los cobros que se generen después.</summary>
    [HttpPut("tarifas")]
    [ProducesResponseType<TarifasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public Task<TarifasDto> Guardar(GuardarTarifasDto datos, CancellationToken cancelacion) =>
        _tarifas.GuardarAsync(datos, UsuarioId, cancelacion);

    /// <summary>Fija desde qué mes se cobra a un club; nulo para no cobrar.</summary>
    [HttpPut("clubes/{clubId:guid}/cobro")]
    [ProducesResponseType<EstadoDePagoClubDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<EstadoDePagoClubDto> FijarInicioDelCobro(
        Guid clubId, FijarInicioDelCobroDto datos, CancellationToken cancelacion) =>
        _inicioDelCobro.FijarAsync(clubId, datos, cancelacion);
}
