namespace LFClub.Infraestructura.Pagos;

/// <summary>
/// Representa la configuración de la pasarela Wompi (spec 017, RF-025 a RF-027).
/// Su responsabilidad es llevar la llave pública, el secreto de integridad, el secreto de eventos y
/// la dirección del checkout, que se leen de <c>Wompi:*</c> de la configuración (variables de entorno
/// o <c>user-secrets</c>) y nunca se escriben en el repositorio.
/// No contiene valores reales ni los muestra: <see cref="Completa"/> solo dice si están todos.
/// </summary>
public class OpcionesWompi
{
    /// <summary>Dirección del checkout de Wompi por defecto.</summary>
    public const string UrlCheckoutPorDefecto = "https://checkout.wompi.co/p/";

    /// <summary>Llave pública del comercio. Va en la dirección de pago.</summary>
    public string LlavePublica { get; set; } = string.Empty;

    /// <summary>Secreto con el que se firma la integridad de la dirección de pago.</summary>
    public string SecretoIntegridad { get; set; } = string.Empty;

    /// <summary>Secreto con el que Wompi firma las notificaciones.</summary>
    public string SecretoEventos { get; set; } = string.Empty;

    /// <summary>Dirección del checkout.</summary>
    public string UrlCheckout { get; set; } = UrlCheckoutPorDefecto;

    /// <summary>Verdadero si están la llave pública y los dos secretos.</summary>
    public bool Completa =>
        !string.IsNullOrWhiteSpace(LlavePublica)
        && !string.IsNullOrWhiteSpace(SecretoIntegridad)
        && !string.IsNullOrWhiteSpace(SecretoEventos);
}
