using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los partidos de un club: listarlos, crearlos, consultarlos,
/// cambiarlos, cancelarlos y reactivarlos (spec 009, historias 2 y 3).
/// Su responsabilidad es recibir la petición y delegar en los servicios con el integrante de la
/// petición, que ya resolvió <see cref="IntegranteDelClubAttribute"/>.
/// No contiene reglas de negocio ni decide quién ve qué: un partido próximo al que quien pregunta no
/// tiene acceso responde el mismo <c>404</c> que uno que no existe (§17.2). La convocatoria y el
/// resultado tienen sus propios controladores.
/// </summary>
[Route("api/clubes/{clubId:guid}/partidos")]
[Tags("Partidos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorPartidos : ControladorBase
{
    private readonly IServicioPartidos _partidos;
    private readonly IServicioConsultaPartidos _consulta;
    private readonly IServicioRespuestas _respuestas;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorPartidos(IServicioPartidos partidos, IServicioConsultaPartidos consulta, IServicioRespuestas respuestas)
    {
        _partidos = partidos;
        _consulta = consulta;
        _respuestas = respuestas;
    }

    /// <summary>Los partidos que quien pregunta puede ver, opcionalmente de un estado o una categoría.</summary>
    [HttpGet]
    [IntegranteDelClub]
    [ProducesResponseType<IReadOnlyList<PartidoResumenDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<PartidoResumenDto>> Listar(
        [FromQuery] EstadoPartido? estado, [FromQuery] Guid? categoriaId, CancellationToken cancelacion) =>
        _consulta.ListarAsync(estado, categoriaId, Integrante, cancelacion);

    /// <summary>Crea un partido próximo, sin convocados.</summary>
    [HttpPost]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PartidoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Crear(GuardarPartidoDto datos, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await _partidos.CrearAsync(datos, Integrante, cancelacion));

    /// <summary>El detalle de un partido.</summary>
    [HttpGet("{partidoId:guid}")]
    [IntegranteDelClub]
    [ProducesResponseType<PartidoDto>(StatusCodes.Status200OK)]
    public Task<PartidoDto> Obtener(Guid partidoId, CancellationToken cancelacion) =>
        _consulta.ObtenerAsync(partidoId, Integrante, cancelacion);

    /// <summary>Cambia los datos de un partido próximo.</summary>
    [HttpPut("{partidoId:guid}")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PartidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PartidoDto> Cambiar(Guid partidoId, GuardarPartidoDto datos, CancellationToken cancelacion) =>
        _partidos.CambiarAsync(partidoId, datos, Integrante, cancelacion);

    /// <summary>Cancela un partido próximo.</summary>
    [HttpPost("{partidoId:guid}/cancelacion")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PartidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PartidoDto> Cancelar(Guid partidoId, CancellationToken cancelacion) =>
        _partidos.CancelarAsync(partidoId, Integrante, cancelacion);

    /// <summary>Reactiva un partido cancelado que todavía no empieza.</summary>
    [HttpDelete("{partidoId:guid}/cancelacion")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<PartidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PartidoDto> Reactivar(Guid partidoId, CancellationToken cancelacion) =>
        _partidos.ReactivarAsync(partidoId, Integrante, cancelacion);

    /// <summary>La familia del jugador de la petición dice si asistirá.</summary>
    [HttpPut("{partidoId:guid}/mi-respuesta")]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<PartidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PartidoDto> Responder(Guid partidoId, ResponderDto datos, CancellationToken cancelacion) =>
        _respuestas.ResponderPartidoAsync(partidoId, datos, Integrante, cancelacion);
}
