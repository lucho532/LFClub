using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los archivos de la ficha de un jugador: abrir el archivo entregado
/// para un documento pedido y subirlo o reemplazarlo.
/// Su responsabilidad es recibir la petición, leer el archivo del formulario y delegar en
/// <see cref="IServicioDocumentosJugador"/>. Abren el PRESIDENTE, los DIRECTIVOS y la cuenta del
/// jugador; suben el PRESIDENTE y la cuenta del jugador. El ENTRENADOR no abre ni sube nada
/// (RF-007): <see cref="IntegranteDelClubAttribute"/> le responde <c>403</c>.
/// No contiene reglas de negocio ni valida el archivo. El parámetro <c>{documento}</c> no lleva
/// restricción de ruta, para que la autorización se ejecute antes de leerlo: un valor que no es de
/// la lista responde <c>404</c> en el servicio. El archivo se sirve con sesión, sin caché, con su
/// tipo de contenido detectado al subir y sin dejar que el navegador lo adivine: nunca hay una
/// dirección pública.
/// </summary>
[Route("api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/ficha/documentos/{documento}")]
[Tags("Ficha")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorDocumentosJugador : ControladorBase
{
    private readonly IServicioDocumentosJugador _servicio;

    /// <summary>Crea el controlador con el servicio de los archivos de la ficha.</summary>
    public ControladorDocumentosJugador(IServicioDocumentosJugador servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Abre el archivo entregado para un documento pedido.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.JUGADOR)]
    [Produces("application/pdf", "image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Abrir(Guid usuarioRolId, string documento, CancellationToken cancelacion)
    {
        var (contenido, tipoContenido, nombre) =
            await _servicio.AbrirAsync(usuarioRolId, documento, Integrante, cancelacion);

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentDisposition = $"inline; filename=\"{nombre}\"";
        return File(contenido, tipoContenido);
    }

    /// <summary>Sube el archivo de un documento pedido, o reemplaza el que había: PDF, JPEG, PNG o WebP de hasta 10 MB.</summary>
    [HttpPut]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]
    [RequestSizeLimit(ArchivoCargado.LimiteDePeticionDeDocumento)]
    [RequestFormLimits(MultipartBodyLengthLimit = ArchivoCargado.LimiteDePeticionDeDocumento)]
    [ProducesResponseType<FichaJugadorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status413PayloadTooLarge, Problema.TipoContenido)]
    public async Task<FichaJugadorDto> Subir(
        Guid usuarioRolId, string documento, IFormFile? archivo, CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return await _servicio.SubirAsync(
            usuarioRolId, documento, await ArchivoCargado.LeerAsync(archivo, cancelacion), Integrante, cancelacion);
    }
}
