using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los jugadores de un club en lo que toca a su categoría: la lista
/// "Sin categoría", la lista de retirados, ubicar o cambiar de categoría a un jugador, retirarlo y
/// reincorporarlo.
/// Su responsabilidad es recibir la petición y delegar en los servicios.
/// No contiene reglas de negocio: las listas las ven el PRESIDENTE y los DIRECTIVOS; solo el
/// PRESIDENTE ubica y reincorpora, y retiran el PRESIDENTE y la cuenta del propio jugador, lo que
/// comprueba <see cref="IntegranteDelClubAttribute"/> en cada acción y, para la familia, el
/// servicio de retiro. No devuelve de un jugador su documento, su correo ni su celular: esos datos están en su
/// ficha, que tiene su propio controlador (<see cref="ControladorFichaJugador"/>) y su propia regla
/// de acceso.
/// </summary>
[Route("api/clubes/{clubId:guid}/jugadores")]
[Tags("Jugadores")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorJugadores : ControladorBase
{
    private readonly IServicioConsultaCategorias _consulta;
    private readonly IServicioUbicacionJugador _ubicacion;
    private readonly IServicioRetiroJugador _retiro;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorJugadores(
        IServicioConsultaCategorias consulta, IServicioUbicacionJugador ubicacion, IServicioRetiroJugador retiro)
    {
        _consulta = consulta;
        _ubicacion = ubicacion;
        _retiro = retiro;
    }

    /// <summary>Jugadores aprobados y no retirados que todavía no tienen categoría.</summary>
    [HttpGet("sin-categoria")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]
    [ProducesResponseType<IReadOnlyList<JugadorDeCategoriaDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<JugadorDeCategoriaDto>> ListarSinCategoria(CancellationToken cancelacion) =>
        _consulta.ListarSinCategoriaAsync(cancelacion);

    /// <summary>Jugadores retirados del club.</summary>
    [HttpGet("retirados")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]
    [ProducesResponseType<IReadOnlyList<JugadorRetiradoDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<JugadorRetiradoDto>> ListarRetirados(CancellationToken cancelacion) =>
        _consulta.ListarRetiradosAsync(cancelacion);

    /// <summary>Ubica a un jugador en una categoría o lo pasa a otra.</summary>
    [HttpPut("{usuarioRolId:guid}/categoria")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<CategoriaDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CategoriaDetalleDto> Ubicar(Guid usuarioRolId, UbicarJugadorDto datos, CancellationToken cancelacion) =>
        _ubicacion.UbicarAsync(usuarioRolId, datos, cancelacion);

    /// <summary>
    /// Retira del club a un jugador aprobado: el PRESIDENTE a cualquiera; la cuenta de un jugador,
    /// solo al jugador de la petición (404 con cualquier otro).
    /// </summary>
    [HttpPost("{usuarioRolId:guid}/retiro")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Retirar(Guid usuarioRolId, CancellationToken cancelacion)
    {
        await _retiro.RetirarAsync(usuarioRolId, Integrante, cancelacion);
        return NoContent();
    }

    /// <summary>
    /// Reincorpora a un jugador retirado; solo el PRESIDENTE. Se niega mientras su documento esté
    /// activo con otra cuenta en otro club.
    /// </summary>
    [HttpPost("{usuarioRolId:guid}/reincorporacion")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<ReincorporacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<ReincorporacionDto> Reincorporar(Guid usuarioRolId, CancellationToken cancelacion) =>
        _retiro.ReincorporarAsync(usuarioRolId, cancelacion);
}
