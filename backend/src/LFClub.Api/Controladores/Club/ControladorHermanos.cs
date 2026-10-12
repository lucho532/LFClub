using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa el endpoint con el que la familia agrega un hermano desde la ficha de un jugador
/// (constitución §12.1.2).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioAgregarHermano"/>.
/// No contiene reglas de negocio: solo admite el rol JUGADOR, lo que comprueba
/// <see cref="IntegranteDelClubAttribute"/>, que además niega la petición a un jugador en espera o
/// retirado; un PRESIDENTE, un DIRECTIVO y un ENTRENADOR reciben 403 (RF-007 y RF-008 de la 006).
/// No aprueba ni rechaza al hermano: eso es de los ingresos.
/// </summary>
[Route("api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/hermanos")]
[Tags("Hermanos")]
[IntegranteDelClub(Rol.JUGADOR)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorHermanos : ControladorBase
{
    private readonly IServicioAgregarHermano _servicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ControladorHermanos(IServicioAgregarHermano servicio)
    {
        _servicio = servicio;
    }

    /// <summary>
    /// Agrega un jugador a la cuenta, en este club, y lo deja en la sala de espera. Responde 200,
    /// sin crear nada, si ese documento ya es de un jugador en espera de la misma cuenta. Avisa si
    /// aprobarlo retirará a un jugador de otra cuenta en otro club, sin decir cuál.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<HermanoAgregadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<HermanoAgregadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Agregar(Guid usuarioRolId, AgregarHermanoDto datos, CancellationToken cancelacion)
    {
        var (hermano, creado) = await _servicio.AgregarAsync(usuarioRolId, Integrante, datos, cancelacion);
        return StatusCode(creado ? StatusCodes.Status201Created : StatusCodes.Status200OK, hermano);
    }
}
