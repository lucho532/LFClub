using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de la ficha básica de un PRESIDENTE, DIRECTIVO o ENTRENADOR del club y
/// de su foto de perfil ante el club (spec 008).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioFichaBasica"/>. Los
/// pide cualquier rol del club, también la cuenta de un jugador, sin límite de categoría (RF-003);
/// <see cref="IntegranteDelClubAttribute"/> aplica los 403 de pertenencia de siempre.
/// No contiene reglas de negocio. La foto se sirve con sesión y sin caché: nunca hay una dirección
/// pública (§14.2).
/// </summary>
[Route("api/clubes/{clubId:guid}/personas/{usuarioRolId:guid}")]
[Tags("Personas")]
[IntegranteDelClub]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorPersonas : ControladorBase
{
    private readonly IServicioFichaBasica _servicio;

    /// <summary>Crea el controlador con el servicio de la ficha básica.</summary>
    public ControladorPersonas(IServicioFichaBasica servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Ficha básica de un PRESIDENTE, DIRECTIVO o ENTRENADOR del club.</summary>
    [HttpGet]
    [ProducesResponseType<FichaBasicaDto>(StatusCodes.Status200OK)]
    public Task<FichaBasicaDto> Obtener(Guid usuarioRolId, CancellationToken cancelacion) =>
        _servicio.ObtenerAsync(usuarioRolId, cancelacion);

    /// <summary>Foto de perfil de un PRESIDENTE, DIRECTIVO o ENTRENADOR del club.</summary>
    [HttpGet("foto")]
    [Produces("image/png", "image/jpeg", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerFoto(Guid usuarioRolId, CancellationToken cancelacion)
    {
        var (contenido, tipoContenido) = await _servicio.ObtenerFotoAsync(usuarioRolId, cancelacion);

        // Es un dato personal: ni cachés compartidas ni copias en disco.
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(contenido, tipoContenido);
    }
}
