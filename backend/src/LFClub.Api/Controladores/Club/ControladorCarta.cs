using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de la carta de un jugador (spec 010, historias 3 y 4): consultarla,
/// cambiar su dorsal, su posición y sus estadísticas, y descargarla como imagen.
/// Su responsabilidad es recibir la petición y delegar en los servicios con el integrante de la
/// petición. La carta la ven exactamente quienes ven la ficha; a los demás se les responde
/// <c>404</c>, igual que si no existiera.
/// No contiene reglas de negocio ni decide quién ve o cambia qué. Sus respuestas no se guardan en
/// caché: son datos de menores.
/// </summary>
[Route("api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/carta")]
[Tags("Carta")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorCarta : ControladorBase
{
    private readonly IServicioConsultaCarta _consulta;
    private readonly IServicioCarta _carta;
    private readonly IServicioImagenCarta _imagen;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorCarta(IServicioConsultaCarta consulta, IServicioCarta carta, IServicioImagenCarta imagen)
    {
        _consulta = consulta;
        _carta = carta;
        _imagen = imagen;
    }

    /// <summary>La carta del jugador, con lo que quien pregunta puede hacer con ella.</summary>
    [HttpGet]
    [IntegranteDelClub]
    [ProducesResponseType<CartaDto>(StatusCodes.Status200OK)]
    public Task<CartaDto> Obtener(Guid usuarioRolId, CancellationToken cancelacion)
    {
        SinCache();
        return _consulta.ObtenerAsync(usuarioRolId, Integrante, cancelacion);
    }

    /// <summary>Pone o cambia el dorsal y la posición.</summary>
    [HttpPut("dorsal-y-posicion")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR, Rol.JUGADOR)]
    [ProducesResponseType<CartaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CartaDto> CambiarDorsalYPosicion(Guid usuarioRolId, DorsalYPosicionDto datos, CancellationToken cancelacion)
    {
        SinCache();
        return _carta.CambiarDorsalYPosicionAsync(usuarioRolId, datos, Integrante, cancelacion);
    }

    /// <summary>Pone, cambia o quita las seis estadísticas.</summary>
    [HttpPut("estadisticas")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR, Rol.JUGADOR)]
    [ProducesResponseType<CartaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CartaDto> CambiarEstadisticas(Guid usuarioRolId, EstadisticasCartaDto datos, CancellationToken cancelacion)
    {
        SinCache();
        return _carta.CambiarEstadisticasAsync(usuarioRolId, datos, Integrante, cancelacion);
    }

    /// <summary>La carta como imagen PNG para descargar: solo la familia del jugador y el PRESIDENTE.</summary>
    [HttpGet("imagen")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
    public async Task<IActionResult> Imagen(Guid usuarioRolId, CancellationToken cancelacion)
    {
        var imagen = await _imagen.GenerarAsync(usuarioRolId, Integrante, cancelacion);
        SinCache();
        return File(imagen.Contenido, "image/png", imagen.NombreArchivo);
    }

    /// <summary>La carta no se guarda en cachés compartidas ni en disco.</summary>
    private void SinCache() => Response.Headers.CacheControl = "private, no-store";
}
