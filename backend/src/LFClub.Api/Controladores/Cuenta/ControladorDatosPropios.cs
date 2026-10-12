using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Cuenta;

/// <summary>
/// Representa los endpoints de los datos propios de "Mi perfil" de la cuenta con sesión (spec 008).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioDatosPropios"/>.
/// No contiene reglas de negocio. Ninguna ruta lleva identificador de cuenta ni de club: los datos
/// son siempre los de la sesión, así que no hay forma de leer ni cambiar los de otra persona.
/// </summary>
[Route("api/cuenta/datos-propios")]
[Tags("Cuenta")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorDatosPropios : ControladorBase
{
    private readonly IServicioDatosPropios _servicio;

    /// <summary>Crea el controlador con el servicio de los datos propios.</summary>
    public ControladorDatosPropios(IServicioDatosPropios servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Datos personales de la cuenta con sesión, si es PRESIDENTE, DIRECTIVO o ENTRENADOR en algún club.</summary>
    [HttpGet]
    [ProducesResponseType<DatosPropiosDto>(StatusCodes.Status200OK)]
    public Task<DatosPropiosDto> Obtener(CancellationToken cancelacion) =>
        _servicio.ObtenerAsync(UsuarioId, cancelacion);

    /// <summary>Cambia nombres, apellidos, fecha de nacimiento y celular en todos los clubes de la cuenta.</summary>
    [HttpPut]
    [ProducesResponseType<DatosPropiosDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public Task<DatosPropiosDto> Cambiar(CambiarDatosPropiosDto datos, CancellationToken cancelacion) =>
        _servicio.CambiarAsync(UsuarioId, datos, cancelacion);
}
