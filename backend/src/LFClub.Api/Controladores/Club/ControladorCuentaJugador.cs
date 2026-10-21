using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa el endpoint del estado de cuenta de un jugador (spec 015, RF-001 a RF-004).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioCuentaDelJugador"/>,
/// pasándole el integrante de la sesión. Lo consultan el PRESIDENTE y el DIRECTIVO, de cualquier
/// jugador del club, y la cuenta de un jugador, solo del suyo. El ENTRENADOR no ve nada de esto:
/// <see cref="IntegranteDelClubAttribute"/> le responde <c>403</c>. Lo que es de otra familia o de otro
/// club responde <c>404</c> en el servicio.
/// No contiene reglas de negocio.
/// </summary>
[Route("api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/cuenta")]
[Tags("Pagos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorCuentaJugador : ControladorBase
{
    private readonly IServicioCuentaDelJugador _servicio;

    /// <summary>Crea el controlador con el servicio de la cuenta del jugador.</summary>
    public ControladorCuentaJugador(IServicioCuentaDelJugador servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Cargos, pagos, saldo y estado de un jugador.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.JUGADOR)]
    [ProducesResponseType<CuentaJugadorDto>(StatusCodes.Status200OK)]
    public Task<CuentaJugadorDto> Obtener(Guid usuarioRolId, CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return _servicio.ObtenerAsync(usuarioRolId, Integrante, cancelacion);
    }
}
