using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla del estado de pago de un club que muestra el panel (spec 017, data-model).
/// Su responsabilidad es derivar <see cref="EstadoDePagoClub"/> del estado y el motivo del club, su
/// mes de inicio y sus cobros pendientes.
/// No guarda nada: se calcula al consultar.
/// </summary>
public static class ReglaEstadoDePagoClub
{
    /// <summary>Calcula el estado de pago del club.</summary>
    public static EstadoDePagoClub Calcular(
        EstadoClub estado,
        MotivoSuspension? motivo,
        DateOnly? mesInicioCobro,
        IEnumerable<CobroParaSuspension> cobros,
        DateOnly hoy)
    {
        if (estado == EstadoClub.SUSPENDIDO && motivo == MotivoSuspension.IMPAGO)
        {
            return EstadoDePagoClub.SUSPENDIDO_POR_IMPAGO;
        }

        var pendientes = cobros.Where(cobro => cobro.Estado == EstadoCobroPlataforma.PENDIENTE).ToList();
        if (pendientes.Count > 0)
        {
            return pendientes.Any(cobro => cobro.SuspendeDesde <= hoy)
                ? EstadoDePagoClub.VENCIDO
                : EstadoDePagoClub.PENDIENTE;
        }

        var primerDiaDelMes = new DateOnly(hoy.Year, hoy.Month, 1);
        return mesInicioCobro is null || mesInicioCobro > primerDiaDelMes
            ? EstadoDePagoClub.SIN_COBRO
            : EstadoDePagoClub.AL_DIA;
    }
}
