namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado actual de un pago (constitución §16.3 y §18; spec 015, RF-018 a RF-023).
/// Su responsabilidad es distinguir el pago informado que espera revisión del confirmado, el
/// rechazado y el anulado; solo el confirmado suma al estado de cuenta.
/// No guarda historial de estados: las transiciones admitidas las decide
/// <c>ReglaTransicionPago</c> y cada paso deja quién y cuándo en el propio pago.
/// </summary>
public enum EstadoPago
{
    /// <summary>Informado por la familia; no suma, no tiene recibo.</summary>
    EN_REVISION,

    /// <summary>Confirmado y aplicado a cargos; es inmutable y solo se anula.</summary>
    CONFIRMADO,

    /// <summary>Rechazado con un motivo; es final y no suma.</summary>
    RECHAZADO,

    /// <summary>Confirmado y luego anulado con un motivo; es final y deja de sumar.</summary>
    ANULADO,
}
