using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los equipos de una categoría: crear, renombrar, desactivar y borrar
/// un equipo, y poner o sacar de él a un jugador.
/// Su responsabilidad es recibir la petición y delegar en los servicios.
/// No contiene reglas de negocio: solo admite al PRESIDENTE del club de la ruta, lo que comprueba
/// <see cref="IntegranteDelClubAttribute"/>; ni siquiera el entrenador de la categoría decide en
/// qué equipo juega cada jugador (RF-025). No ofrece ninguna operación para pasar un equipo a otra
/// categoría ni para reactivarlo.
/// </summary>
[Route("api/clubes/{clubId:guid}/categorias/{categoriaId:guid}/equipos")]
[Tags("Categorias")]
[IntegranteDelClub(Rol.PRESIDENTE)]
[ProducesResponseType<CategoriaDetalleDto>(StatusCodes.Status200OK)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorEquipos : ControladorBase
{
    private readonly IServicioEquipos _equipos;
    private readonly IServicioJugadoresDeEquipo _jugadores;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorEquipos(IServicioEquipos equipos, IServicioJugadoresDeEquipo jugadores)
    {
        _equipos = equipos;
        _jugadores = jugadores;
    }

    /// <summary>Crea un equipo dentro de la categoría.</summary>
    [HttpPost]
    [ProducesResponseType<CategoriaDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Crear(Guid categoriaId, NombreEquipoDto datos, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await _equipos.CrearAsync(categoriaId, datos, cancelacion));

    /// <summary>Cambia el nombre de un equipo.</summary>
    [HttpPut("{equipoId:guid}")]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CategoriaDetalleDto> Renombrar(
        Guid categoriaId, Guid equipoId, NombreEquipoDto datos, CancellationToken cancelacion) =>
        _equipos.RenombrarAsync(categoriaId, equipoId, datos, cancelacion);

    /// <summary>Borra un equipo que nunca ha tenido jugadores ni entrenadores.</summary>
    [HttpDelete("{equipoId:guid}")]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CategoriaDetalleDto> Borrar(Guid categoriaId, Guid equipoId, CancellationToken cancelacion) =>
        _equipos.BorrarAsync(categoriaId, equipoId, cancelacion);

    /// <summary>Desactiva un equipo.</summary>
    [HttpPost("{equipoId:guid}/desactivacion")]
    public Task<CategoriaDetalleDto> Desactivar(Guid categoriaId, Guid equipoId, CancellationToken cancelacion) =>
        _equipos.DesactivarAsync(categoriaId, equipoId, cancelacion);

    /// <summary>Pone a un jugador de la categoría en un equipo.</summary>
    [HttpPut("{equipoId:guid}/jugadores/{usuarioRolId:guid}")]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CategoriaDetalleDto> PonerJugador(
        Guid categoriaId, Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion) =>
        _jugadores.PonerAsync(categoriaId, equipoId, usuarioRolId, cancelacion);

    /// <summary>Saca a un jugador de un equipo.</summary>
    [HttpDelete("{equipoId:guid}/jugadores/{usuarioRolId:guid}")]
    public Task<CategoriaDetalleDto> SacarJugador(
        Guid categoriaId, Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion) =>
        _jugadores.SacarAsync(categoriaId, equipoId, usuarioRolId, cancelacion);
}
