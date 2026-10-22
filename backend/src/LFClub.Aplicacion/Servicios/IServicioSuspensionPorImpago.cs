namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la suspensión automática por falta de pago y la vuelta a la normalidad (spec 017,
/// RF-016 a RF-019).
/// Su responsabilidad es aplicar <c>ReglaSuspensionPorImpago</c> a los clubes: suspender por impago al
/// que venció su plazo y reactivar al que estaba suspendido por impago y ya no debe nada vencido.
/// No toca una suspensión manual ni un club dado de baja, y no confirma pagos.
/// </summary>
public interface IServicioSuspensionPorImpago
{
    /// <summary>Revisa todos los clubes con un cobro pendiente o suspendidos por impago. Devuelve cuántos suspendió.</summary>
    Task<int> AplicarAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Revisa un club y lo reactiva o suspende si corresponde. Se llama dentro de la transacción que
    /// confirma un pago, marca uno a mano o anula un cobro, para que la vuelta sea inmediata.
    /// </summary>
    Task ReevaluarAsync(Guid clubId, CancellationToken cancelacion = default);
}
