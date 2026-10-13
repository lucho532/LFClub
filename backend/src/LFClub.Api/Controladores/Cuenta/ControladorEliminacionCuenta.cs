using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Cuenta;

/// <summary>
/// Representa los endpoints de "Eliminar mi cuenta" de la cuenta con sesión (spec 014).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioEliminacionCuenta"/>.
/// No contiene reglas de negocio. Ninguna ruta lleva identificador de cuenta ni de club: siempre es
/// la cuenta de la sesión, así que no hay forma de actuar sobre otra persona.
/// </summary>
[Route("api/cuenta/eliminacion")]
[Tags("Cuenta")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorEliminacionCuenta : ControladorBase
{
    private readonly IServicioEliminacionCuenta _servicio;

    /// <summary>Crea el controlador con el servicio de eliminación de la cuenta.</summary>
    public ControladorEliminacionCuenta(IServicioEliminacionCuenta servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Qué jugadores se irían con la cuenta y si algún club lo impide.</summary>
    [HttpGet]
    [ProducesResponseType<ResumenEliminacionCuentaDto>(StatusCodes.Status200OK)]
    public Task<ResumenEliminacionCuentaDto> Resumen(CancellationToken cancelacion) =>
        _servicio.ObtenerResumenAsync(UsuarioId, cancelacion);

    /// <summary>Elimina la cuenta, sus integrantes y jugadores en todos los clubes.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Eliminar(EliminarCuentaDto datos, CancellationToken cancelacion)
    {
        await _servicio.EliminarAsync(UsuarioId, datos, cancelacion);
        return NoContent();
    }
}
