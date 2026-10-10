using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa el endpoint con el que la cuenta de un jugador ve su categoría, sus equipos y el
/// nombre de sus entrenadores.
/// Su responsabilidad es recibir la petición y delegar en el servicio, pasándole el integrante de
/// la sesión.
/// No contiene reglas de negocio y no recibe ningún identificador de categoría ni de jugador: solo
/// admite al rol JUGADOR, lo que comprueba <see cref="IntegranteDelClubAttribute"/>, y responde
/// siempre sobre el propio jugador.
/// </summary>
[Route("api/clubes/{clubId:guid}/mi-categoria")]
[Tags("Jugadores")]
[IntegranteDelClub(Rol.JUGADOR)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorMiCategoria : ControladorBase
{
    private readonly IServicioMiCategoria _servicio;

    /// <summary>Crea el controlador con el servicio de "Mi categoría".</summary>
    public ControladorMiCategoria(IServicioMiCategoria servicio)
    {
        _servicio = servicio;
    }

    /// <summary>
    /// La categoría, los equipos y los entrenadores del jugador que hace la petición: el único de
    /// la cuenta en el club o, si tiene varios, el elegido.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<MiCategoriaDto>(StatusCodes.Status200OK)]
    public Task<MiCategoriaDto> Obtener(CancellationToken cancelacion) => _servicio.ObtenerAsync(Integrante, cancelacion);
}
