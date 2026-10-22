namespace LFClub.Aplicacion.Interfaces;

/// <summary>Lo que hace falta para armar la dirección de pago de un intento.</summary>
/// <param name="Referencia">Referencia única del intento.</param>
/// <param name="ValorEnPesos">Valor del cobro, en pesos enteros.</param>
/// <param name="DireccionDeRetorno">Dirección a la que vuelve la persona después de pagar.</param>
public record SolicitudDePago(string Referencia, decimal ValorEnPesos, string DireccionDeRetorno);

/// <summary>Una notificación de la pasarela cuya firma ya se comprobó.</summary>
/// <param name="TransaccionId">Identificador de la transacción.</param>
/// <param name="Estado">Estado informado (APPROVED, DECLINED…).</param>
/// <param name="Referencia">Referencia del intento.</param>
/// <param name="ValorEnPesos">Valor pagado, en pesos.</param>
/// <param name="MedioDePago">Medio informado (PSE, NEQUI, CARD…), si viene.</param>
public record NotificacionVerificada(
    string TransaccionId, string Estado, string Referencia, decimal ValorEnPesos, string? MedioDePago);

/// <summary>
/// Representa la pasarela de pago del cobro de la plataforma (constitución §6.2).
/// Su responsabilidad es armar la dirección de pago firmada y comprobar la firma de las
/// notificaciones, sin hacer llamadas a la pasarela.
/// No decide si un cobro está pagado ni guarda nada.
/// </summary>
public interface IPasarelaDePago
{
    /// <summary>Verdadero si la configuración está completa y se puede cobrar en línea.</summary>
    bool Disponible { get; }

    /// <summary>La dirección de pago ya firmada, o nulo si la pasarela no está configurada.</summary>
    Uri? CrearEnlaceDePago(SolicitudDePago solicitud);

    /// <summary>
    /// Lee la notificación y devuelve sus datos solo si la firma es válida; nulo si no lo es o si el
    /// cuerpo no se puede leer.
    /// </summary>
    NotificacionVerificada? VerificarNotificacion(string cuerpo, string? checksumDeLaCabecera);
}
