using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de registrar un pago recibido directamente y de consultar un pago con su
/// recibo y su resguardo (spec 015, RF-020, RF-024 y RF-025).
/// Su responsabilidad es recibir la petición y delegar en <see cref="IServicioRegistroDePagos"/> y
/// <see cref="IServicioConsultaDePagos"/>, pasándoles el integrante de la sesión. Registra solo el
/// PRESIDENTE; consultan el PRESIDENTE, el DIRECTIVO y la cuenta del jugador del pago. El ENTRENADOR no
/// ve nada de esto: <see cref="IntegranteDelClubAttribute"/> le responde <c>403</c>. No existe ninguna
/// operación que edite o borre un pago confirmado (§16.3).
/// No contiene reglas de negocio.
/// </summary>
[Tags("Pagos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorPagos : ControladorBase
{
    private readonly IServicioRegistroDePagos _registro;
    private readonly IServicioConsultaDePagos _consulta;

    /// <summary>Crea el controlador con los servicios de registro y consulta de pagos.</summary>
    public ControladorPagos(IServicioRegistroDePagos registro, IServicioConsultaDePagos consulta)
    {
        _registro = registro;
        _consulta = consulta;
    }

    /// <summary>Registra un pago recibido directamente: nace confirmado y aplicado a los cargos.</summary>
    [HttpPost("api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/pagos")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<PagoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Registrar(
        Guid usuarioRolId, RegistrarPagoDto datos, CancellationToken cancelacion) =>
        StatusCode(
            StatusCodes.Status201Created, await _registro.RegistrarAsync(usuarioRolId, datos, Integrante, cancelacion));

    /// <summary>El detalle de un pago y su recibo.</summary>
    [HttpGet("api/clubes/{clubId:guid}/pagos/{pagoId:guid}")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.JUGADOR)]
    [ProducesResponseType<PagoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public Task<PagoDto> Obtener(Guid pagoId, CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return _consulta.ObtenerAsync(pagoId, Integrante, cancelacion);
    }

    /// <summary>El resguardo de un pago como imagen PNG de 1080 × 1350, dibujada al pedirla.</summary>
    [HttpGet("api/clubes/{clubId:guid}/pagos/{pagoId:guid}/resguardo")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.JUGADOR)]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public async Task<IActionResult> AbrirResguardo(Guid pagoId, CancellationToken cancelacion)
    {
        var imagen = await _consulta.AbrirResguardoAsync(pagoId, Integrante, cancelacion);

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(imagen, "image/png", $"resguardo-{pagoId:N}.png");
    }
}
