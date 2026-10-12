using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de la foto propia de un jugador (spec 007): verla, cargarla o
/// cambiarla y quitarla.
/// Su responsabilidad es recibir la petición, leer el archivo del formulario y delegar en
/// <see cref="IServicioFotoJugador"/>. Pueden pedir la foto todos los roles del club, y el servicio
/// la entrega solo con el alcance de la ficha; cargan y quitan el PRESIDENTE y la cuenta del
/// jugador, y a los DIRECTIVOS y ENTRENADORES <see cref="IntegranteDelClubAttribute"/> les
/// responde <c>403</c>.
/// No contiene reglas de negocio ni valida la imagen. La foto se sirve con sesión y sin caché:
/// nunca hay una dirección pública (§14.2).
/// </summary>
[Route("api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/foto")]
[Tags("Ficha")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorFotoJugador : ControladorBase
{
    private readonly IServicioFotoJugador _servicio;

    /// <summary>Crea el controlador con el servicio de la foto del jugador.</summary>
    public ControladorFotoJugador(IServicioFotoJugador servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Devuelve la foto del jugador.</summary>
    [HttpGet]
    [IntegranteDelClub]
    [Produces("image/png", "image/jpeg", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(Guid usuarioRolId, CancellationToken cancelacion)
    {
        var (contenido, tipoContenido) = await _servicio.ObtenerAsync(usuarioRolId, Integrante, cancelacion);

        // Es la foto de un menor: ni cachés compartidas ni copias en disco.
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(contenido, tipoContenido);
    }

    /// <summary>Carga o reemplaza la foto del jugador: PNG, JPEG o WebP de hasta 1 MB.</summary>
    [HttpPut]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]
    [RequestSizeLimit(ArchivoCargado.LimiteDePeticion)]
    [RequestFormLimits(MultipartBodyLengthLimit = ArchivoCargado.LimiteDePeticion)]
    [ProducesResponseType<FichaJugadorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<FichaJugadorDto> Guardar(Guid usuarioRolId, IFormFile? archivo, CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return await _servicio.GuardarAsync(
            usuarioRolId, await ArchivoCargado.LeerAsync(archivo, cancelacion), Integrante, cancelacion);
    }

    /// <summary>Quita la foto del jugador; si no tenía, no cambia nada.</summary>
    [HttpDelete]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]
    [ProducesResponseType<FichaJugadorDto>(StatusCodes.Status200OK)]
    public Task<FichaJugadorDto> Quitar(Guid usuarioRolId, CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return _servicio.QuitarAsync(usuarioRolId, Integrante, cancelacion);
    }
}
