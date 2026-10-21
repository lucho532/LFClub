using System.Globalization;
using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de las transferencias que informa la familia (spec 015, RF-021, RF-022 y
/// RF-030): informar con un soporte, abrir el soporte, ver los pagos en revisión, confirmarlos y
/// rechazarlos, y anular uno confirmado (RF-019).
/// Su responsabilidad es recibir la petición, leer la imagen del formulario y delegar en
/// <see cref="IServicioRevisionDePagos"/> y <see cref="IServicioConsultaDePagos"/>. Informa solo la
/// cuenta del propio jugador; ven la lista el PRESIDENTE y el DIRECTIVO; confirma y rechaza solo el
/// PRESIDENTE. El ENTRENADOR no ve nada de esto: <see cref="IntegranteDelClubAttribute"/> le responde
/// <c>403</c>. La imagen de soporte se sirve con sesión, sin caché y sin dejar que el navegador adivine su
/// tipo: nunca hay una dirección pública.
/// No contiene reglas de negocio ni valida la imagen.
/// </summary>
[Tags("Pagos")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
public class ControladorRevisionDePagos : ControladorBase
{
    private readonly IServicioRevisionDePagos _revision;
    private readonly IServicioConsultaDePagos _consulta;
    private readonly IServicioAnulacionDePagos _anulacion;

    /// <summary>Crea el controlador con los servicios de revisión, consulta y anulación de pagos.</summary>
    public ControladorRevisionDePagos(
        IServicioRevisionDePagos revision, IServicioConsultaDePagos consulta, IServicioAnulacionDePagos anulacion)
    {
        _revision = revision;
        _consulta = consulta;
        _anulacion = anulacion;
    }

    /// <summary>La familia informa una transferencia de su jugador con la imagen del comprobante.</summary>
    [HttpPost("api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/pagos/informados")]
    [IntegranteDelClub(Rol.JUGADOR)]
    [RequestSizeLimit(ArchivoCargado.LimiteDePeticionDeSoporte)]
    [RequestFormLimits(MultipartBodyLengthLimit = ArchivoCargado.LimiteDePeticionDeSoporte)]
    [ProducesResponseType<PagoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Informar(
        Guid usuarioRolId,
        [FromForm] string? valor,
        [FromForm] string? entidad,
        [FromForm] string? referencia,
        [FromForm] DateOnly fecha,
        IFormFile? archivo,
        CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        var pago = await _revision.InformarAsync(
            usuarioRolId,
            new InformarPagoDto(ValorDelFormulario(valor), entidad, referencia, fecha),
            await ArchivoCargado.LeerAsync(archivo, cancelacion),
            Integrante,
            cancelacion);
        return StatusCode(StatusCodes.Status201Created, pago);
    }

    /// <summary>
    /// El valor escrito en el formulario, leído siempre con punto decimal: no depende de la cultura del
    /// servidor. Lo que no es un número queda en cero y el validador responde 400.
    /// </summary>
    private static decimal ValorDelFormulario(string? valor) =>
        decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero) ? numero : 0m;

    /// <summary>La imagen de soporte de un pago.</summary>
    [HttpGet("api/clubes/{clubId:guid}/pagos/{pagoId:guid}/soporte")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.JUGADOR)]
    [Produces("image/png", "image/jpeg", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    public async Task<IActionResult> AbrirSoporte(Guid pagoId, CancellationToken cancelacion)
    {
        var (contenido, tipoContenido) = await _consulta.AbrirSoporteAsync(pagoId, Integrante, cancelacion);

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(contenido, tipoContenido);
    }

    /// <summary>Los pagos que esperan revisión.</summary>
    [HttpGet("api/clubes/{clubId:guid}/pagos/en-revision")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]
    [ProducesResponseType<PagosEnRevisionDto>(StatusCodes.Status200OK)]
    public Task<PagosEnRevisionDto> ListarEnRevision(CancellationToken cancelacion)
    {
        Response.Headers.CacheControl = "private, no-store";
        return _revision.ListarEnRevisionAsync(cancelacion);
    }

    /// <summary>Confirma un pago en revisión, con el valor y los cargos que decida el PRESIDENTE.</summary>
    [HttpPost("api/clubes/{clubId:guid}/pagos/{pagoId:guid}/confirmacion")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<PagoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PagoDto> Confirmar(Guid pagoId, [FromBody] ConfirmarPagoDto? datos, CancellationToken cancelacion) =>
        _revision.ConfirmarAsync(pagoId, datos, Integrante, cancelacion);

    /// <summary>Rechaza un pago en revisión con un motivo.</summary>
    [HttpPost("api/clubes/{clubId:guid}/pagos/{pagoId:guid}/rechazo")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<PagoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PagoDto> Rechazar(Guid pagoId, MotivoDto datos, CancellationToken cancelacion) =>
        _revision.RechazarAsync(pagoId, datos, Integrante, cancelacion);

    /// <summary>
    /// Anula un pago confirmado con un motivo: sigue visible, marcado como anulado, y el saldo se recalcula.
    /// Es la única forma de deshacer un pago confirmado (§16.3).
    /// </summary>
    [HttpPost("api/clubes/{clubId:guid}/pagos/{pagoId:guid}/anulacion")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<PagoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<PagoDto> Anular(Guid pagoId, MotivoDto datos, CancellationToken cancelacion) =>
        _anulacion.AnularAsync(pagoId, datos, Integrante, cancelacion);
}
