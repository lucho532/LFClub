using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints con los que el PRESIDENTE cambia la identidad de su club: colores y
/// escudo (spec 007, historia 4).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioIdentidadClub"/>, que
/// aplica las mismas reglas que el panel del DESARROLLADOR (<c>ControladorIdentidadClub</c>).
/// No contiene reglas de negocio ni valida la imagen. Solo admite al PRESIDENTE del club de la
/// ruta, también con el club suspendido; a otro rol <see cref="IntegranteDelClubAttribute"/> le
/// responde <c>403</c>, y a quien no es del club, <c>404</c>.
/// </summary>
[Route("api/clubes/{clubId:guid}")]
[Tags("Club")]
[IntegranteDelClub(Rol.PRESIDENTE)]
[ProducesResponseType<ClubDto>(StatusCodes.Status200OK)]
[ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorIdentidadDelClub : ControladorBase
{
    private readonly IServicioIdentidadClub _servicio;

    /// <summary>Crea el controlador con el servicio de identidad.</summary>
    public ControladorIdentidadDelClub(IServicioIdentidadClub servicio)
    {
        _servicio = servicio;
    }

    /// <summary>El PRESIDENTE define los colores de su club.</summary>
    [HttpPut("colores")]
    public Task<ClubDto> ActualizarColores(ActualizarColoresDto datos, CancellationToken cancelacion) =>
        _servicio.ActualizarColoresDelClubAsync(datos, Integrante, cancelacion);

    /// <summary>El PRESIDENTE carga o reemplaza el escudo de su club: PNG, JPEG o WebP de hasta 1 MB.</summary>
    [HttpPut("escudo")]
    [RequestSizeLimit(ArchivoCargado.LimiteDePeticion)]
    [RequestFormLimits(MultipartBodyLengthLimit = ArchivoCargado.LimiteDePeticion)]
    public async Task<ClubDto> ActualizarEscudo(IFormFile? archivo, CancellationToken cancelacion) =>
        await _servicio.ActualizarEscudoDelClubAsync(
            await ArchivoCargado.LeerAsync(archivo, cancelacion), Integrante, cancelacion);
}
