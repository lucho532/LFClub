namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa lo que pasó con una notificación de la pasarela (spec 017, research §9).
/// Su responsabilidad es nombrar el desenlace para auditarlo y mostrar al DESARROLLADOR los casos
/// raros (duplicados, valores que no coinciden, firmas inválidas).
/// No guarda el contenido de la notificación.
/// </summary>
public enum ResultadoNotificacion
{
    /// <summary>Pago aprobado y aplicado: el cobro quedó pagado.</summary>
    APLICADA,

    /// <summary>La misma transacción y estado ya se habían recibido; no se tocó nada.</summary>
    REPETIDA,

    /// <summary>El estado de la transacción no es aprobado; no se tocó nada.</summary>
    NO_APROBADA,

    /// <summary>El cobro ya tenía un pago; el segundo queda registrado para devolverlo.</summary>
    PAGO_DUPLICADO,

    /// <summary>Se pagó un cobro anulado; queda registrado para devolverlo.</summary>
    PAGO_A_COBRO_ANULADO,

    /// <summary>El valor pagado no es el del cobro; no se aplicó.</summary>
    VALOR_NO_COINCIDE,

    /// <summary>La referencia no corresponde a ningún intento de pago.</summary>
    REFERENCIA_DESCONOCIDA,

    /// <summary>La firma no era válida; se rechazó sin leer nada del contenido.</summary>
    FIRMA_INVALIDA,
}
