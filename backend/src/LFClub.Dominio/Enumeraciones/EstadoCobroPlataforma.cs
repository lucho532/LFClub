namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado del cobro mensual de la plataforma a un club (spec 017).
/// Su responsabilidad es nombrar los estados posibles; <c>PAGADO</c> y <c>ANULADO</c> son finales.
/// No define quién puede cambiarlos ni cuándo.
/// </summary>
public enum EstadoCobroPlataforma
{
    /// <summary>Generado y sin pagar.</summary>
    PENDIENTE,

    /// <summary>Pagado por la pasarela (notificación verificada) o marcado a mano por el DESARROLLADOR.</summary>
    PAGADO,

    /// <summary>Anulado por el DESARROLLADOR, con motivo; ya no se puede pagar.</summary>
    ANULADO,
}
