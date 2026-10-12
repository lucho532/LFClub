using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Plataforma;

/// <summary>
/// Representa los endpoints del panel de administración sobre los clubes: listar, crear, ver el
/// detalle y editar sus datos.
/// Su responsabilidad es recibir la petición y delegar en los servicios.
/// No contiene reglas de negocio; solo admite a la cuenta DESARROLLADOR.
/// </summary>
[Route("api/plataforma/clubes")]
[Tags("Plataforma")]
[SoloDesarrollador]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorClubes : ControladorBase
{
    private readonly IServicioConsultaClubes _consulta;
    private readonly IServicioCreacionClub _creacion;
    private readonly IServicioConfiguracionClub _configuracion;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorClubes(
        IServicioConsultaClubes consulta, IServicioCreacionClub creacion, IServicioConfiguracionClub configuracion)
    {
        _consulta = consulta;
        _creacion = creacion;
        _configuracion = configuracion;
    }

    /// <summary>Lista de todos los clubes con su estado.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClubResumenDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ClubResumenDto>> Listar(CancellationToken cancelacion) =>
        _consulta.ListarAsync(cancelacion);

    /// <summary>Crea un club e invita a su presidente.</summary>
    [HttpPost]
    [ProducesResponseType<ClubDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Crear(CrearClubDto datos, CancellationToken cancelacion)
    {
        var club = await _creacion.CrearAsync(datos, UsuarioId, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { clubId = club.ClubId }, club);
    }

    /// <summary>Detalle de un club, con sus presidentes y sus invitaciones sin usar.</summary>
    [HttpGet("{clubId:guid}")]
    [ProducesResponseType<ClubDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<ClubDetalleDto> Obtener(Guid clubId, CancellationToken cancelacion) =>
        _consulta.ObtenerDetalleAsync(clubId, cancelacion);

    /// <summary>El DESARROLLADOR edita nombre, sede, dirección y contacto de un club.</summary>
    [HttpPut("{clubId:guid}/configuracion")]
    [ProducesResponseType<ClubDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<ClubDetalleDto> ActualizarConfiguracion(
        Guid clubId, ActualizarConfiguracionClubDto datos, CancellationToken cancelacion) =>
        _configuracion.ActualizarDesdeElPanelAsync(clubId, datos, cancelacion);
}
