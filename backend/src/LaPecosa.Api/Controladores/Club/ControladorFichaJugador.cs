using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de la ficha de un jugador: consultarla, guardar su formulario, cambiar
/// su documento de identidad y corregir su identidad.
/// Su responsabilidad es recibir la petición y delegar en los servicios. El PRESIDENTE ve y cambia
/// la ficha completa de cualquier jugador de su club, con categoría, sin ella o retirado; el
/// DIRECTIVO ve cualquier ficha sin los datos clínicos, también si entrena la categoría, y no
/// cambia nada; el ENTRENADOR ve, sin los documentos y sin cambiar nada, la de los jugadores
/// activos de las categorías que entrena; y la cuenta de un jugador ve y cambia la del jugador
/// con el que continúa, y no la de un hermano mientras no lo elija (RF-030 de la 006), menos los
/// nombres, los apellidos y la fecha de nacimiento, que solo corrige el PRESIDENTE.
/// No contiene reglas de negocio ni decide quién ve qué: <see cref="IntegranteDelClubAttribute"/>
/// niega con <c>403</c> al rol que nunca puede hacer la operación, y los servicios responden
/// <c>404</c> a quien no puede ver esa ficha, igual que si no existiera. No sirve los archivos de
/// la ficha (ver <c>ControladorDocumentosJugador</c>). Sus respuestas no se guardan en
/// caché: son datos de menores.
/// </summary>
[Route("api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/ficha")]
[Tags("Ficha")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorFichaJugador : ControladorBase
{
    private readonly IServicioConsultaFicha _consulta;
    private readonly IServicioFichaJugador _ficha;
    private readonly IServicioIdentidadJugador _identidad;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorFichaJugador(
        IServicioConsultaFicha consulta, IServicioFichaJugador ficha, IServicioIdentidadJugador identidad)
    {
        _consulta = consulta;
        _ficha = ficha;
        _identidad = identidad;
    }

    /// <summary>La ficha de un jugador, con lo que el rol de quien pregunta permite ver.</summary>
    [HttpGet]
    [IntegranteDelClub]
    [ProducesResponseType<FichaJugadorDto>(StatusCodes.Status200OK)]
    public Task<FichaJugadorDto> Obtener(Guid usuarioRolId, CancellationToken cancelacion)
    {
        SinCache();
        return _consulta.ObtenerAsync(usuarioRolId, Integrante, cancelacion);
    }

    /// <summary>Guarda el contacto, el contacto de emergencia, la seguridad social y los datos clínicos.</summary>
    [HttpPut]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]
    [ProducesResponseType<FichaJugadorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public Task<FichaJugadorDto> Actualizar(Guid usuarioRolId, ActualizarFichaDto datos, CancellationToken cancelacion)
    {
        SinCache();
        return _ficha.ActualizarAsync(usuarioRolId, datos, Integrante, cancelacion);
    }

    /// <summary>Cambia el tipo y el número del documento de identidad del jugador.</summary>
    [HttpPut("documento-identidad")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]
    [ProducesResponseType<FichaJugadorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<FichaJugadorDto> CambiarDocumentoDeIdentidad(
        Guid usuarioRolId, CambiarDocumentoIdentidadDto datos, CancellationToken cancelacion)
    {
        SinCache();
        return _identidad.CambiarDocumentoAsync(usuarioRolId, datos, Integrante, cancelacion);
    }

    /// <summary>Corrige los nombres, los apellidos y la fecha de nacimiento del jugador.</summary>
    [HttpPut("identidad")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<FichaJugadorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public Task<FichaJugadorDto> CorregirIdentidad(
        Guid usuarioRolId, CorregirIdentidadDto datos, CancellationToken cancelacion)
    {
        SinCache();
        return _identidad.CorregirAsync(usuarioRolId, datos, Integrante, cancelacion);
    }

    /// <summary>La ficha no se guarda en cachés compartidas ni en disco.</summary>
    private void SinCache() => Response.Headers.CacheControl = "private, no-store";
}
