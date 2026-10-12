using LFClub.Api.Errores;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Publico;

/// <summary>
/// Representa el endpoint público del escudo de un club.
/// Su responsabilidad es fijar en <see cref="IContextoClub"/> el club de la ruta y servir su
/// escudo, sin sesión: una etiqueta de imagen no envía la cabecera de sesión y la pantalla de
/// registro lo necesita antes de que exista la cuenta (research §10).
/// No es una excepción al aislamiento (lee con el filtro activo) y no devuelve ningún otro dato
/// del club. Es de los pocos endpoints anónimos, y lo es de forma explícita (constitución §9).
/// </summary>
[Route("api/publico/clubes/{clubId:guid}/escudo")]
[Tags("Publico")]
[AllowAnonymous]
public class ControladorEscudo : ControladorBase
{
    private readonly IServicioEscudoPublico _servicio;
    private readonly IContextoClub _contextoClub;

    /// <summary>Crea el controlador con el servicio del escudo y el contexto del club.</summary>
    public ControladorEscudo(IServicioEscudoPublico servicio, IContextoClub contextoClub)
    {
        _servicio = servicio;
        _contextoClub = contextoClub;
    }

    /// <summary>Imagen del escudo del club. El parámetro <c>v</c> solo sirve para la caché.</summary>
    [HttpGet]
    [Produces("image/png", "image/jpeg", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public async Task<IActionResult> Obtener(Guid clubId, [FromQuery] int? v, CancellationToken cancelacion)
    {
        _contextoClub.Fijar(clubId);
        var (contenido, tipoContenido) = await _servicio.ObtenerAsync(cancelacion);

        // Con versión, la dirección cambia cada vez que cambia el escudo: se puede guardar para siempre.
        Response.Headers.CacheControl = v is null ? "no-cache" : "public, max-age=31536000, immutable";
        return File(contenido, tipoContenido);
    }
}
