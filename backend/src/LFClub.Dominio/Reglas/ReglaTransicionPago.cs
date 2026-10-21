using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de las transiciones de estado de un pago (spec 015, RF-018 a RF-023;
/// constitución §16.3 y §18).
/// Su responsabilidad es decir qué se puede hacer desde cada estado: un pago en revisión se confirma
/// o se rechaza; uno confirmado solo se anula; rechazado y anulado son finales. Un pago confirmado no
/// se edita ni se borra, así que no hay otra transición.
/// No comprueba quién lo pide ni cambia el estado.
/// </summary>
public static class ReglaTransicionPago
{
    /// <summary>Código cuando se intenta confirmar o rechazar un pago que no está en revisión.</summary>
    public const string NoEstaEnRevision = "pago_no_esta_en_revision";

    /// <summary>Código cuando se intenta anular un pago que no está confirmado.</summary>
    public const string NoEstaConfirmado = "pago_no_esta_confirmado";

    /// <summary>En revisión → confirmado.</summary>
    public static bool PuedeConfirmar(EstadoPago estado) => estado == EstadoPago.EN_REVISION;

    /// <summary>En revisión → rechazado.</summary>
    public static bool PuedeRechazar(EstadoPago estado) => estado == EstadoPago.EN_REVISION;

    /// <summary>Confirmado → anulado.</summary>
    public static bool PuedeAnular(EstadoPago estado) => estado == EstadoPago.CONFIRMADO;

    /// <summary>Indica si el pago tiene recibo y resguardo: está confirmado o fue anulado.</summary>
    public static bool TieneRecibo(EstadoPago estado) => estado is EstadoPago.CONFIRMADO or EstadoPago.ANULADO;

    /// <summary>Indica si el pago suma al estado de cuenta: solo el confirmado.</summary>
    public static bool Suma(EstadoPago estado) => estado == EstadoPago.CONFIRMADO;
}
