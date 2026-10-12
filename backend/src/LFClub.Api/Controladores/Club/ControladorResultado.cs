using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa el endpoint del resultado de un partido (spec 009, historia 3).
/// Su responsabilidad es recibir el marcador y lo que jugó cada convocado y delegar en el servicio
/// con el integrante de la petición.
/// No contiene reglas de negocio ni decide quién puede registrarlo.
/// </summary>
[Route("api/clubes/{clubId:guid}/partidos/{partidoId:guid}/resultado")]
[Tags("Partidos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorResultado : ControladorBase
{
    private readonly IServicioResultado _resultado;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ControladorResultado(IServicioResultado resultado)
    {
        _resultado = resultado;
    }

    /// <summary>Registra o corrige el resultado; deja el partido jugado.</summary>
    [HttpPut]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PartidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PartidoDto> Registrar(Guid partidoId, RegistrarResultadoDto datos, CancellationToken cancelacion) =>
        _resultado.RegistrarAsync(partidoId, datos, Integrante, cancelacion);
}
