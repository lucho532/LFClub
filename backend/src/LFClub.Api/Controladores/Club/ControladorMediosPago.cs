using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los medios por los que el club recibe pagos (spec 015, RF-026 a RF-028).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioMediosPago"/>. Leen el
/// PRESIDENTE, el DIRECTIVO (todos) y la cuenta de un jugador (solo los activos); escribe solo el
/// PRESIDENTE. El ENTRENADOR no ve nada de esto: <see cref="IntegranteDelClubAttribute"/> le responde
/// <c>403</c>.
/// No contiene reglas de negocio.
/// </summary>
[Route("api/clubes/{clubId:guid}/medios-de-pago")]
[Tags("MediosPago")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorMediosPago : ControladorBase
{
    private readonly IServicioMediosPago _servicio;

    /// <summary>Crea el controlador con el servicio de los medios de pago.</summary>
    public ControladorMediosPago(IServicioMediosPago servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Los medios de pago que ve quien pregunta.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.JUGADOR)]
    [ProducesResponseType<IReadOnlyList<MedioPagoDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MedioPagoDto>> Listar(CancellationToken cancelacion) =>
        _servicio.ListarAsync(Integrante, cancelacion);

    /// <summary>Añade un medio de pago.</summary>
    [HttpPost]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<MedioPagoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    public async Task<IActionResult> Crear(GuardarMedioPagoDto datos, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.CrearAsync(datos, cancelacion));

    /// <summary>Edita o desactiva un medio de pago.</summary>
    [HttpPut("{medioId:guid}")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<MedioPagoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<MedioPagoDto> Editar(Guid medioId, GuardarMedioPagoDto datos, CancellationToken cancelacion) =>
        _servicio.EditarAsync(medioId, datos, cancelacion);

    /// <summary>Quita un medio de pago; no toca ningún pago.</summary>
    [HttpDelete("{medioId:guid}")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public async Task<IActionResult> Quitar(Guid medioId, CancellationToken cancelacion)
    {
        await _servicio.QuitarAsync(medioId, cancelacion);
        return NoContent();
    }
}
