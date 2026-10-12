using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa el endpoint con el que la familia de un convocado publica el resultado de un partido
/// o indica lo de su jugador (spec 010, historia 1).
/// Su responsabilidad es recibir el marcador (si todavía no lo hay) y lo de su jugador, y delegar
/// en el servicio con el integrante de la petición.
/// No contiene reglas de negocio ni decide quién puede publicar.
/// </summary>
[Route("api/clubes/{clubId:guid}/partidos/{partidoId:guid}/resultado/mi-jugador")]
[Tags("Partidos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorResultadoFamilia : ControladorBase
{
    private readonly IServicioResultadoFamilia _resultado;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ControladorResultadoFamilia(IServicioResultadoFamilia resultado)
    {
        _resultado = resultado;
    }

    /// <summary>Publica el resultado o indica lo del jugador de la petición; responde el partido como lo ve la familia.</summary>
    [HttpPut]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<PartidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PartidoDto> Publicar(Guid partidoId, ResultadoDeFamiliaDto datos, CancellationToken cancelacion) =>
        _resultado.PublicarAsync(partidoId, datos, Integrante, cancelacion);
}
