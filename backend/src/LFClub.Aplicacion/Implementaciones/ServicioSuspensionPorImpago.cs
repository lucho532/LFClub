using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que suspende y reactiva clubes por falta de pago (spec 017, RF-016 a RF-019).
/// Su responsabilidad es aplicar <see cref="ReglaSuspensionPorImpago"/> con el día de hoy en Colombia,
/// bloqueando la fila del club, y guardar el estado nuevo con el motivo <c>IMPAGO</c> y sin autor
/// (lo hizo el sistema).
/// No decide los plazos ni confirma pagos, y nunca toca una suspensión manual.
/// </summary>
public class ServicioSuspensionPorImpago : IServicioSuspensionPorImpago
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioSuspensionPorImpago(
        IRepositorioClubesPlataforma clubes,
        IRepositorioCobrosPlataforma cobros,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _clubes = clubes;
        _cobros = cobros;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<int> AplicarAsync(CancellationToken cancelacion = default)
    {
        var suspendidos = 0;
        foreach (var clubId in await _cobros.ListarClubesParaRevisarAsync(cancelacion))
        {
            var decision = await _unidadDeTrabajo.EnTransaccionAsync(
                () => EvaluarAsync(clubId, cancelacion), cancelacion);
            if (decision == DecisionDeSuspension.Suspender)
            {
                suspendidos++;
            }
        }

        return suspendidos;
    }

    /// <inheritdoc />
    public Task ReevaluarAsync(Guid clubId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(() => EvaluarAsync(clubId, cancelacion), cancelacion);

    private async Task<DecisionDeSuspension> EvaluarAsync(Guid clubId, CancellationToken cancelacion)
    {
        var club = await _clubes.ObtenerBloqueandoAsync(clubId, cancelacion);
        if (club is null)
        {
            return DecisionDeSuspension.SinCambio;
        }

        var pendientes = await _cobros.ListarPendientesAsync(clubId, cancelacion);
        var decision = ReglaSuspensionPorImpago.Evaluar(
            club.Estado,
            club.MotivoSuspension,
            pendientes.Select(cobro => new CobroParaSuspension(cobro.Estado, cobro.SuspendeDesde, cobro.SuspensionOmitida)),
            ReglaMesDeCobro.DiaEnColombia(_reloj.AhoraUtc));

        if (decision == DecisionDeSuspension.SinCambio)
        {
            return decision;
        }

        club.Estado = decision == DecisionDeSuspension.Suspender ? EstadoClub.SUSPENDIDO : EstadoClub.ACTIVO;
        club.MotivoSuspension = decision == DecisionDeSuspension.Suspender ? MotivoSuspension.IMPAGO : null;
        club.EstadoCambiadoPorUsuarioId = null;
        club.EstadoCambiadoEn = _reloj.AhoraUtc;
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
        return decision;
    }
}
