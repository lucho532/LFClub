namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa la situación de pago de un club frente a la plataforma (spec 017, data-model).
/// Su responsabilidad es nombrar los estados que el panel del DESARROLLADOR muestra; se calculan
/// al consultar y no se guardan.
/// No decide si el club se suspende: eso lo decide la regla de suspensión por impago.
/// </summary>
public enum EstadoDePagoClub
{
    /// <summary>No se le cobra todavía: sin mes de inicio o con el mes de inicio en el futuro.</summary>
    SIN_COBRO,

    /// <summary>No tiene ningún cobro pendiente.</summary>
    AL_DIA,

    /// <summary>Tiene un cobro pendiente dentro del plazo.</summary>
    PENDIENTE,

    /// <summary>Tiene un cobro pendiente cuyo plazo ya venció.</summary>
    VENCIDO,

    /// <summary>El club está suspendido por falta de pago.</summary>
    SUSPENDIDO_POR_IMPAGO,
}
