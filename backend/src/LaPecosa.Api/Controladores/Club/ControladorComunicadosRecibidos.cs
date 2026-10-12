using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los comunicados del lado de la cuenta de un jugador: el detalle y
/// la lectura de un comunicado recibido (spec 011, historias 2 y 3). La lista, el contador y
/// "marcar todos" pasaron a la campana unificada, <see cref="ControladorCampana"/> (spec 012).
/// Su responsabilidad es recibir la petición y delegar en los servicios con el integrante de la
/// petición y la limitación de la sesión, para que una sesión iniciada con el documento de un
/// jugador solo vea lo de ese jugador.
/// No contiene reglas de negocio y nunca devuelve la audiencia ni cuántas familias lo recibieron.
/// Solo la cuenta de un jugador entra: los demás roles reciben <c>403 rol_no_autorizado</c>.
/// </summary>
[Route("api/clubes/{clubId:guid}/comunicados/recibidos")]
[Tags("Comunicados")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorComunicadosRecibidos : ControladorBase
{
    private readonly IServicioConsultaComunicados _consulta;
    private readonly IServicioLecturaComunicados _lectura;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorComunicadosRecibidos(IServicioConsultaComunicados consulta, IServicioLecturaComunicados lectura)
    {
        _consulta = consulta;
        _lectura = lectura;
    }

    /// <summary>Un comunicado recibido; no lo marca como leído.</summary>
    [HttpGet("{comunicadoId:guid}")]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<ComunicadoRecibidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<ComunicadoRecibidoDto> Obtener(Guid comunicadoId, CancellationToken cancelacion) =>
        _consulta.ObtenerRecibidoAsync(comunicadoId, Integrante, Limitacion, cancelacion);

    /// <summary>Marca un comunicado como leído para toda la cuenta; es idempotente.</summary>
    [HttpPut("{comunicadoId:guid}/lectura")]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<ComunicadoRecibidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<ComunicadoRecibidoDto> MarcarLeido(Guid comunicadoId, CancellationToken cancelacion) =>
        _lectura.MarcarLeidoAsync(comunicadoId, Integrante, Limitacion, cancelacion);
}
