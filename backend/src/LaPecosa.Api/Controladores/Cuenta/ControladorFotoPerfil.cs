using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Cuenta;

/// <summary>
/// Representa los endpoints de la foto de perfil de la cuenta con sesión.
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioFotoPerfil"/>.
/// No contiene reglas de negocio. Ninguna ruta lleva identificador de cuenta: la foto es siempre
/// la de la sesión, y es la única vía para cargarla, cambiarla o quitarla (RF-009 de la 008). La de
/// un PRESIDENTE, DIRECTIVO o ENTRENADOR la ven además los integrantes de su club, solo en su ficha
/// básica y en la miniatura junto a su nombre, por <c>ControladorPersonas</c> (§14.2 4.5.0).
/// </summary>
[Route("api/cuenta/foto")]
[Tags("Cuenta")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
public class ControladorFotoPerfil : ControladorBase
{
    private readonly IServicioFotoPerfil _servicio;

    /// <summary>Crea el controlador con el servicio de la foto de perfil.</summary>
    public ControladorFotoPerfil(IServicioFotoPerfil servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Devuelve la foto de perfil de la cuenta con sesión.</summary>
    [HttpGet]
    [Produces("image/png", "image/jpeg", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public async Task<IActionResult> Obtener(CancellationToken cancelacion)
    {
        var (contenido, tipoContenido) = await _servicio.ObtenerAsync(UsuarioId, cancelacion);

        // Es un dato personal: ni cachés compartidas ni copias en disco.
        Response.Headers.CacheControl = "private, no-store";
        return File(contenido, tipoContenido);
    }

    /// <summary>Carga o reemplaza la foto de perfil: PNG, JPEG o WebP de hasta 1 MB.</summary>
    [HttpPut]
    [RequestSizeLimit(ArchivoCargado.LimiteDePeticion)]
    [RequestFormLimits(MultipartBodyLengthLimit = ArchivoCargado.LimiteDePeticion)]
    [ProducesResponseType<SesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public async Task<SesionDto> Guardar(IFormFile? archivo, CancellationToken cancelacion) =>
        await _servicio.GuardarAsync(
            UsuarioId, await ArchivoCargado.LeerAsync(archivo, cancelacion), Limitacion, cancelacion);

    /// <summary>Quita la foto de perfil de la cuenta con sesión.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Quitar(CancellationToken cancelacion)
    {
        await _servicio.QuitarAsync(UsuarioId, cancelacion);
        return NoContent();
    }
}
