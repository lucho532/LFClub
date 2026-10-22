using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que cambia el estado de un club.
/// Su responsabilidad es aplicar la regla de transiciones y guardar el estado nuevo junto con
/// quién lo cambió y cuándo (RF-031), y el motivo de la suspensión (spec 017): una suspensión puesta aquí
/// es siempre <c>MANUAL</c>, y cualquier otro cambio vacía el motivo. Si el DESARROLLADOR levanta a mano
/// una suspensión por impago, los cobros vencidos del club se marcan con la suspensión omitida (siguen
/// pendientes y pagables, pero ya no suspenden): el club queda activo hasta el siguiente cobro.
/// No toca integrantes, invitaciones ni configuración: suspender y levantar la suspensión deja el
/// club exactamente como estaba (CE-007). El efecto sobre el acceso lo aplica la autorización en
/// cada petición.
/// </summary>
public class ServicioEstadoClub : IServicioEstadoClub
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaClubes _consulta;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioEstadoClub(
        IRepositorioClubesPlataforma clubes,
        IRepositorioCobrosPlataforma cobros,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaClubes consulta,
        IReloj reloj)
    {
        _clubes = clubes;
        _cobros = cobros;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<ClubDetalleDto> CambiarAsync(
        Guid clubId, CambiarEstadoClubDto datos, Guid usuarioId, CancellationToken cancelacion = default)
    {
        if (datos.Estado is null || !Enum.IsDefined(datos.Estado.Value))
        {
            var errores = new ErroresDeValidacion();
            errores.Agregar("estado", "Indica el estado al que pasa el club.");
            errores.LanzarSiHayErrores();
        }

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var club = await _clubes.ObtenerBloqueandoAsync(clubId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();

                if (!ReglaTransicionEstadoClub.SePermite(club.Estado, datos.Estado!.Value))
                {
                    throw ExcepcionDeAplicacion.Conflicto(
                        "transicion_no_permitida", "El club no puede pasar a ese estado desde su estado actual.");
                }

                var eraImpago = club.Estado == EstadoClub.SUSPENDIDO && club.MotivoSuspension == MotivoSuspension.IMPAGO;
                club.Estado = datos.Estado.Value;
                club.MotivoSuspension = club.Estado == EstadoClub.SUSPENDIDO ? MotivoSuspension.MANUAL : null;
                if (eraImpago && club.Estado == EstadoClub.ACTIVO)
                {
                    await OmitirSuspensionDeLosCobrosVencidosAsync(clubId, cancelacion);
                }

                club.EstadoCambiadoPorUsuarioId = usuarioId;
                club.EstadoCambiadoEn = _reloj.AhoraUtc;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerDetalleAsync(clubId, cancelacion);
    }

    private async Task OmitirSuspensionDeLosCobrosVencidosAsync(Guid clubId, CancellationToken cancelacion)
    {
        var hoy = ReglaMesDeCobro.DiaEnColombia(_reloj.AhoraUtc);
        foreach (var cobro in await _cobros.ListarPendientesAsync(clubId, cancelacion))
        {
            if (cobro.SuspendeDesde <= hoy)
            {
                cobro.SuspensionOmitida = true;
            }
        }
    }
}
