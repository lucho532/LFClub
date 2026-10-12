using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los comunicados del lado del equipo del club: publicar, ver lo
/// publicado y retirar (spec 011, historias 1 y 4).
/// Su responsabilidad es recibir la petición y delegar en los servicios con el integrante de la
/// petición, que ya resolvió <see cref="IntegranteDelClubAttribute"/>.
/// No contiene reglas de negocio ni decide la audiencia: la autorización niega con <c>403</c> al
/// rol que nunca puede hacer la operación, y los servicios responden <c>404</c> a quien no ve el
/// comunicado, igual que si no existiera.
/// </summary>
[Route("api/clubes/{clubId:guid}/comunicados")]
[Tags("Comunicados")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorComunicados : ControladorBase
{
    private readonly IServicioPublicacionComunicados _publicacion;
    private readonly IServicioConsultaComunicados _consulta;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorComunicados(IServicioPublicacionComunicados publicacion, IServicioConsultaComunicados consulta)
    {
        _publicacion = publicacion;
        _consulta = consulta;
    }

    /// <summary>Publica un comunicado; repetir la clave devuelve el ya publicado con 200.</summary>
    [HttpPost]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<ComunicadoPublicadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ComunicadoPublicadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Publicar(PublicarComunicadoDto datos, CancellationToken cancelacion)
    {
        var resultado = await _publicacion.PublicarAsync(datos, Integrante, cancelacion);
        return StatusCode(
            resultado.Creado ? StatusCodes.Status201Created : StatusCodes.Status200OK, resultado.Comunicado);
    }

    /// <summary>Lo que publicó el equipo del club, del más reciente al más antiguo.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PaginaDePublicadosDto>(StatusCodes.Status200OK)]
    public Task<PaginaDePublicadosDto> Listar(
        [FromQuery] int? limite, [FromQuery] Guid? antesDe, CancellationToken cancelacion) =>
        _consulta.ListarPublicadosAsync(limite, antesDe, Integrante, cancelacion);

    /// <summary>Retira un comunicado: quien lo publicó y el PRESIDENTE.</summary>
    [HttpPost("{comunicadoId:guid}/retiro")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<ComunicadoPublicadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<ComunicadoPublicadoDto> Retirar(Guid comunicadoId, CancellationToken cancelacion) =>
        _publicacion.RetirarAsync(comunicadoId, Integrante, cancelacion);
}
