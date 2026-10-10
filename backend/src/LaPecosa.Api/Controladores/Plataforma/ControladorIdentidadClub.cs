using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Plataforma;

/// <summary>
/// Representa los endpoints del panel de administración para la identidad de un club: colores y
/// escudo.
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioIdentidadClub"/>.
/// No contiene reglas de negocio ni valida la imagen; solo admite a la cuenta DESARROLLADOR. No es
/// la única entrada: desde la 007 el PRESIDENTE cambia la identidad de su propio club con
/// <c>ControladorIdentidadDelClub</c>, con las mismas reglas, y vale el último cambio.
/// </summary>
[Route("api/plataforma/clubes/{clubId:guid}")]
[Tags("Plataforma")]
[SoloDesarrollador]
[ProducesResponseType<ClubDetalleDto>(StatusCodes.Status200OK)]
[ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorIdentidadClub : ControladorBase
{
    private readonly IServicioIdentidadClub _servicio;

    /// <summary>Crea el controlador con el servicio de identidad.</summary>
    public ControladorIdentidadClub(IServicioIdentidadClub servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Define los colores del club.</summary>
    [HttpPut("colores")]
    public Task<ClubDetalleDto> ActualizarColores(
        Guid clubId, ActualizarColoresDto datos, CancellationToken cancelacion) =>
        _servicio.ActualizarColoresAsync(clubId, datos, cancelacion);

    /// <summary>Carga o reemplaza el escudo del club: PNG, JPEG o WebP de hasta 1 MB.</summary>
    [HttpPut("escudo")]
    [RequestSizeLimit(ArchivoCargado.LimiteDePeticion)]
    [RequestFormLimits(MultipartBodyLengthLimit = ArchivoCargado.LimiteDePeticion)]
    public async Task<ClubDetalleDto> GuardarEscudo(Guid clubId, IFormFile? archivo, CancellationToken cancelacion) =>
        await _servicio.GuardarEscudoAsync(clubId, await ArchivoCargado.LeerAsync(archivo, cancelacion), cancelacion);
}
