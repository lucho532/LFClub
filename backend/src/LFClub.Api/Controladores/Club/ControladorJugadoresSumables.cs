using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa el endpoint de búsqueda de jugadores que se pueden sumar a un entrenamiento o a un
/// partido (spec 013, historia 3).
/// Su responsabilidad es recibir el texto y delegar en el servicio con el integrante de la petición.
/// No contiene reglas de negocio: la autorización niega con <c>403</c> a la cuenta de un jugador.
/// </summary>
[Route("api/clubes/{clubId:guid}/actividades/jugadores-sumables")]
[Tags("Actividades")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorJugadoresSumables : ControladorBase
{
    private readonly IServicioJugadoresSumables _sumables;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ControladorJugadoresSumables(IServicioJugadoresSumables sumables)
    {
        _sumables = sumables;
    }

    /// <summary>Hasta 20 jugadores activos del club con categoría que coinciden con el texto.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<IReadOnlyList<JugadorSumableDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public Task<IReadOnlyList<JugadorSumableDto>> Buscar([FromQuery] string? busqueda, CancellationToken cancelacion) =>
        _sumables.BuscarAsync(busqueda, Integrante, cancelacion);
}
