namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa los errores previstos de los pagos (spec 015), con el código estable del contrato.
/// Su responsabilidad es construir cada <see cref="ExcepcionDeAplicacion"/> con su mensaje en español,
/// de modo que los servicios de registro, revisión y anulación digan lo mismo.
/// No decide cuándo ocurre cada error.
/// </summary>
public static class ErroresDePagos
{
    /// <summary>409 <c>pago_excede_deuda</c>: el valor supera lo pendiente; lleva lo que se debe.</summary>
    public static ExcepcionDeAplicacion PagoExcedeDeuda(decimal debe) => new(
        "pago_excede_deuda",
        409,
        $"El pago supera lo que se debe: pendiente $ {TextoDePesos.ConPuntos(debe)}.",
        extras: new Dictionary<string, object> { ["debe"] = debe });

    /// <summary>409 <c>sin_deuda</c>: no hay nada pendiente que cubrir.</summary>
    public static ExcepcionDeAplicacion SinDeuda() =>
        new("sin_deuda", 409, "El jugador no tiene nada pendiente por pagar.", extras: new Dictionary<string, object> { ["debe"] = 0m });

    /// <summary>409 <c>pago_no_esta_en_revision</c>: solo un pago en revisión se confirma o se rechaza.</summary>
    public static ExcepcionDeAplicacion PagoNoEstaEnRevision() =>
        ExcepcionDeAplicacion.Conflicto("pago_no_esta_en_revision", "Este pago ya no está en revisión.");

    /// <summary>409 <c>pago_no_esta_confirmado</c>: solo un pago confirmado se anula.</summary>
    public static ExcepcionDeAplicacion PagoNoEstaConfirmado() =>
        ExcepcionDeAplicacion.Conflicto("pago_no_esta_confirmado", "Este pago no está confirmado, así que no se puede anular.");

    /// <summary>400 por el soporte: sin imagen, que no es una imagen admitida o más grande de lo permitido.</summary>
    public static ExcepcionDeAplicacion Soporte(string codigo) => codigo switch
    {
        "soporte_requerido" => new(codigo, 400, "Adjunta la imagen del comprobante de la transferencia."),
        "soporte_demasiado_grande" => new(codigo, 400, "La imagen del comprobante supera los 5 MB."),
        _ => new("soporte_invalido", 400, "El comprobante debe ser una imagen PNG, JPEG o WebP."),
    };
}
