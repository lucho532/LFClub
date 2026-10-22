namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un intento de pago ya creado (spec 017, RF-008).
/// Su responsabilidad es llevar la dirección del checkout, ya firmada, y la referencia del intento.
/// No contiene secretos: la dirección solo lleva la llave pública y la firma de integridad.
/// </summary>
/// <param name="UrlDePago">Dirección del checkout de la pasarela.</param>
/// <param name="Referencia">Referencia única del intento.</param>
public record IntentoDePagoDto(string UrlDePago, string Referencia);
