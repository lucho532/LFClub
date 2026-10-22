using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta de un pago, su recibo, su soporte y su resguardo (spec 015, RF-024, RF-025 y
/// RF-030).
/// Su responsabilidad es entregar el detalle de un pago a quien puede verlo: el PRESIDENTE, el DIRECTIVO
/// y la cuenta del jugador del pago. Lo demás responde 404 para no revelar que existe.
/// No cambia nada y no condiciona ningún acceso por deber.
/// </summary>
public interface IServicioConsultaDePagos
{
    /// <summary>El pago con su recibo. De otro jugador, otra familia u otro club: 404.</summary>
    Task<PagoDto> ObtenerAsync(Guid pagoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>
    /// La imagen de soporte de un pago, con su tipo de contenido. Solo la familia del jugador, el PRESIDENTE
    /// y el DIRECTIVO la abren; para cualquier otra cuenta, o si el pago no tiene soporte, es 404. Se
    /// conserva aunque el pago se rechace o se anule.
    /// </summary>
    Task<(byte[] Contenido, string TipoContenido)> AbrirSoporteAsync(
        Guid pagoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>
    /// El resguardo de un pago como imagen PNG, dibujada al pedirla. Existe mientras el pago esté
    /// confirmado o anulado (con el sello ANULADO) y tenga jugador; en revisión, rechazado, o con la
    /// cuenta del jugador eliminada, es 404. Lo ven el PRESIDENTE, el DIRECTIVO y la cuenta del jugador.
    /// </summary>
    Task<byte[]> AbrirResguardoAsync(Guid pagoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
