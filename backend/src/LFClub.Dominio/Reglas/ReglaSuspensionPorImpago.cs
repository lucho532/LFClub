using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>Lo que la regla necesita saber de un cobro para decidir la suspensión.</summary>
public record CobroParaSuspension(EstadoCobroPlataforma Estado, DateOnly SuspendeDesde, bool SuspensionOmitida);

/// <summary>Lo que hay que hacer con un club según sus cobros.</summary>
public enum DecisionDeSuspension
{
    /// <summary>No tocar el club.</summary>
    SinCambio,

    /// <summary>Suspenderlo por impago.</summary>
    Suspender,

    /// <summary>Reactivarlo: estaba suspendido por impago y ya no le queda un cobro vencido.</summary>
    Reactivar,
}

/// <summary>
/// Representa la regla de la suspensión por falta de pago (spec 017, RF-016 a RF-019).
/// Su responsabilidad es decidir si un club se suspende, se reactiva o no cambia, según su estado,
/// el motivo de su suspensión, sus cobros y el día de hoy en Colombia. Una suspensión manual y un
/// club dado de baja nunca cambian.
/// No cambia el estado del club ni lee el reloj: recibe el día.
/// </summary>
public static class ReglaSuspensionPorImpago
{
    /// <summary>Decide qué hacer con el club.</summary>
    public static DecisionDeSuspension Evaluar(
        EstadoClub estado,
        MotivoSuspension? motivo,
        IEnumerable<CobroParaSuspension> cobros,
        DateOnly hoy)
    {
        var hayVencido = cobros.Any(cobro =>
            cobro.Estado == EstadoCobroPlataforma.PENDIENTE
            && cobro.SuspendeDesde <= hoy
            && !cobro.SuspensionOmitida);

        return estado switch
        {
            EstadoClub.ACTIVO when hayVencido => DecisionDeSuspension.Suspender,
            EstadoClub.SUSPENDIDO when motivo == MotivoSuspension.IMPAGO && !hayVencido =>
                DecisionDeSuspension.Reactivar,
            _ => DecisionDeSuspension.SinCambio,
        };
    }
}
