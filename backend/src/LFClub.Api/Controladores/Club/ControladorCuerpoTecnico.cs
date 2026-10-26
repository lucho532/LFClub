using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints del cuerpo técnico del club (spec 018): la lista de sus presidentes, directivos y
/// entrenadores y todos los datos de cada uno.
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioCuerpoTecnico"/>.
/// No contiene reglas de negocio: solo admite al PRESIDENTE del club de la ruta, lo que comprueba
/// <see cref="IntegranteDelClubAttribute"/>; cualquier otro rol recibe 403. Las respuestas llevan datos
/// personales y no se guardan en caché.
/// </summary>
[Route("api/clubes/{clubId:guid}/cuerpo-tecnico")]
[Tags("CuerpoTecnico")]
[IntegranteDelClub(Rol.PRESIDENTE)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorCuerpoTecnico : ControladorBase
{
    private readonly IServicioCuerpoTecnico _servicio;

    /// <summary>Crea el controlador con el servicio del cuerpo técnico.</summary>
    public ControladorCuerpoTecnico(IServicioCuerpoTecnico servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Los presidentes, directivos y entrenadores del club.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MiembroCuerpoTecnicoDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<MiembroCuerpoTecnicoDto>> Listar(CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return await _servicio.ListarAsync(cancelacion);
    }

    /// <summary>Todos los datos de una persona del cuerpo técnico y, si entrena, sus próximos entrenamientos.</summary>
    [HttpGet("{usuarioRolId:guid}")]
    [ProducesResponseType<MiembroCuerpoTecnicoDetalleDto>(StatusCodes.Status200OK)]
    public async Task<MiembroCuerpoTecnicoDetalleDto> Obtener(Guid usuarioRolId, CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return await _servicio.ObtenerAsync(usuarioRolId, cancelacion);
    }
}
