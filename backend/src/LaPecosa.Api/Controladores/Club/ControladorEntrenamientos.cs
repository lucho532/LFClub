using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los entrenamientos de un club: programarlos, consultarlos,
/// cambiarlos, cancelarlos y reactivarlos, y los próximos del inicio (spec 009, historia 1).
/// Su responsabilidad es recibir la petición y delegar en los servicios con el integrante de la
/// petición, que ya resolvió <see cref="IntegranteDelClubAttribute"/>.
/// No contiene reglas de negocio ni decide quién ve qué: la autorización niega con <c>403</c> al rol
/// que nunca puede hacer la operación, y los servicios responden <c>404</c> a quien no ve el
/// entrenamiento, igual que si no existiera, y <c>403 no_gestiona_actividad</c> a quien lo ve pero
/// no lo gestiona.
/// </summary>
[Route("api/clubes/{clubId:guid}/entrenamientos")]
[Tags("Entrenamientos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorEntrenamientos : ControladorBase
{
    private readonly IServicioEntrenamientos _entrenamientos;
    private readonly IServicioConsultaEntrenamientos _consulta;
    private readonly IServicioRespuestas _respuestas;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorEntrenamientos(
        IServicioEntrenamientos entrenamientos, IServicioConsultaEntrenamientos consulta, IServicioRespuestas respuestas)
    {
        _entrenamientos = entrenamientos;
        _consulta = consulta;
        _respuestas = respuestas;
    }

    /// <summary>Programa un entrenamiento para una o varias categorías.</summary>
    [HttpPost]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<EntrenamientoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Programar(GuardarEntrenamientoDto datos, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await _entrenamientos.ProgramarAsync(datos, Integrante, cancelacion));

    /// <summary>Hasta cinco entrenamientos visibles que no han terminado, para el inicio.</summary>
    [HttpGet("proximos")]
    [IntegranteDelClub]
    [ProducesResponseType<IReadOnlyList<EntrenamientoResumenDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<EntrenamientoResumenDto>> Proximos(CancellationToken cancelacion) =>
        _consulta.ProximosAsync(Integrante, cancelacion);

    /// <summary>El detalle de un entrenamiento.</summary>
    [HttpGet("{entrenamientoId:guid}")]
    [IntegranteDelClub]
    [ProducesResponseType<EntrenamientoDto>(StatusCodes.Status200OK)]
    public Task<EntrenamientoDto> Obtener(Guid entrenamientoId, CancellationToken cancelacion) =>
        _consulta.ObtenerAsync(entrenamientoId, Integrante, cancelacion);

    /// <summary>Cambia fecha, horas, lugar y nota de un entrenamiento programado.</summary>
    [HttpPut("{entrenamientoId:guid}")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<EntrenamientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<EntrenamientoDto> Cambiar(Guid entrenamientoId, GuardarEntrenamientoDto datos, CancellationToken cancelacion) =>
        _entrenamientos.CambiarAsync(entrenamientoId, datos, Integrante, cancelacion);

    /// <summary>Cancela un entrenamiento programado.</summary>
    [HttpPost("{entrenamientoId:guid}/cancelacion")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<EntrenamientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<EntrenamientoDto> Cancelar(Guid entrenamientoId, CancellationToken cancelacion) =>
        _entrenamientos.CancelarAsync(entrenamientoId, Integrante, cancelacion);

    /// <summary>Reactiva un entrenamiento cancelado que todavía no empieza.</summary>
    [HttpDelete("{entrenamientoId:guid}/cancelacion")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<EntrenamientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<EntrenamientoDto> Reactivar(Guid entrenamientoId, CancellationToken cancelacion) =>
        _entrenamientos.ReactivarAsync(entrenamientoId, Integrante, cancelacion);

    /// <summary>La familia del jugador de la petición dice si asistirá.</summary>
    [HttpPut("{entrenamientoId:guid}/mi-respuesta")]
    [IntegranteDelClub(Rol.JUGADOR)]
    [ProducesResponseType<EntrenamientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<EntrenamientoDto> Responder(Guid entrenamientoId, ResponderDto datos, CancellationToken cancelacion) =>
        _respuestas.ResponderEntrenamientoAsync(entrenamientoId, datos, Integrante, cancelacion);
}
