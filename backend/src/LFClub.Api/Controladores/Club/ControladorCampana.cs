using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de la campana de la cuenta de un jugador: la lista que mezcla
/// comunicados y avisos, el contador, "marcar todos" y la lectura de un aviso (spec 012, historia 3).
/// Su responsabilidad es recibir la petición y delegar en los servicios con el integrante de la
/// petición y la limitación de la sesión, para que una sesión iniciada con el documento de un
/// jugador solo vea lo de ese jugador.
/// No contiene reglas de negocio y nunca devuelve más de la actividad que su tipo, su identificador
/// y si sigue disponible. Solo la cuenta de un jugador entra: los demás roles reciben
/// <c>403 rol_no_autorizado</c>.
/// </summary>
[Route("api/clubes/{clubId:guid}/campana")]
[Tags("Campana")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorCampana : ControladorBase
{
    private readonly IServicioConsultaCampana _consulta;
    private readonly IServicioLecturaCampana _lectura;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorCampana(IServicioConsultaCampana consulta, IServicioLecturaCampana lectura)
    {
        _consulta = consulta;
        _lectura = lectura;
    }

    /// <summary>Comunicados y avisos que le llegan a la cuenta, del más reciente al más antiguo.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<PaginaDeCampanaDto>(StatusCodes.Status200OK)]
    public Task<PaginaDeCampanaDto> Listar(
        [FromQuery] int? limite, [FromQuery] Guid? antesDe, CancellationToken cancelacion) =>
        _consulta.ListarAsync(limite, antesDe, Integrante, Limitacion, cancelacion);

    /// <summary>Cuántos elementos distintos tiene la cuenta sin leer.</summary>
    [HttpGet("contador")]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<ContadorDeCampanaDto>(StatusCodes.Status200OK)]
    public Task<ContadorDeCampanaDto> Contar(CancellationToken cancelacion) =>
        _consulta.ContarSinLeerAsync(Integrante, Limitacion, cancelacion);

    /// <summary>Marca todos los comunicados y avisos de la cuenta como leídos.</summary>
    [HttpPost("lectura-total")]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<ContadorDeCampanaDto>(StatusCodes.Status200OK)]
    public Task<ContadorDeCampanaDto> MarcarTodos(CancellationToken cancelacion) =>
        _lectura.MarcarTodosAsync(Integrante, Limitacion, cancelacion);

    /// <summary>Marca un aviso como leído para toda la cuenta y dice si su actividad sigue disponible.</summary>
    [HttpPut("avisos/{avisoId:guid}/lectura")]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<AvisoRecibidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<AvisoRecibidoDto> MarcarAvisoLeido(Guid avisoId, CancellationToken cancelacion) =>
        _lectura.MarcarAvisoLeidoAsync(avisoId, Integrante, Limitacion, cancelacion);
}
