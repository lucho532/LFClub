using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el dibujo del resguardo de un pago como imagen PNG (spec 015, RF-025; research §14).
/// Su responsabilidad es convertir los datos del pago, ya autorizados, en una imagen de 1080 × 1350 con
/// la identidad del club, para que la familia la descargue o la comparta.
/// No decide quién la ve, no consulta nada, no la guarda y no añade ningún dato que no reciba.
/// </summary>
public interface IGeneradorImagenResguardo
{
    /// <summary>Los bytes del PNG del resguardo.</summary>
    byte[] Generar(DatosImagenResguardo datos);
}

/// <summary>
/// Representa lo que se dibuja en el resguardo: el recibo del pago y la identidad del club, y nada más.
/// Su responsabilidad es llevarlo junto, con el escudo como bytes.
/// No lleva el documento del jugador, su contacto ni el soporte del pago.
/// </summary>
/// <param name="ClubNombre">Nombre del club.</param>
/// <param name="Escudo">Bytes del escudo del club (PNG, JPEG o WebP), o nulos.</param>
/// <param name="ColorPrincipal">Color principal del club (#RRGGBB), o nulo.</param>
/// <param name="ColorAcento">Color de acento del club (#RRGGBB), o nulo.</param>
/// <param name="NumeroRecibo">Número consecutivo del recibo en el club.</param>
/// <param name="JugadorNombre">Nombres y apellidos del jugador.</param>
/// <param name="Valor">Valor del pago, en pesos.</param>
/// <param name="Medio">Efectivo o transferencia.</param>
/// <param name="Entidad">Entidad de la transferencia, o nula.</param>
/// <param name="Fecha">Fecha en que se pagó.</param>
/// <param name="Concepto">Lo que cubre, en texto, o nulo.</param>
/// <param name="ConfirmadoPorNombre">Quién confirmó el pago.</param>
/// <param name="SaldoTrasPago">Lo que quedó debiendo el jugador; 0 es "Al día".</param>
/// <param name="Anulado">Si el pago se anuló: el resguardo lleva el sello ANULADO.</param>
public record DatosImagenResguardo(
    string ClubNombre,
    byte[]? Escudo,
    string? ColorPrincipal,
    string? ColorAcento,
    int NumeroRecibo,
    string JugadorNombre,
    decimal Valor,
    MedioPago Medio,
    string? Entidad,
    DateOnly Fecha,
    string? Concepto,
    string ConfirmadoPorNombre,
    decimal SaldoTrasPago,
    bool Anulado);
