using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de consultas del panel de administración.
/// Su responsabilidad es reunir el club, sus presidentes, sus invitaciones pendientes y su estado de
/// pago frente a la plataforma (spec 017), y convertirlos en DTO. El estado de pago de la lista sale de
/// una sola consulta de cobros pendientes, no de una por club.
/// No modifica datos, no accede al contexto de Entity Framework y no decide quién puede consultar.
/// </summary>
public class ServicioConsultaClubes : IServicioConsultaClubes
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioInvitacionesPlataforma _invitaciones;
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaClubes(
        IRepositorioClubesPlataforma clubes,
        IRepositorioInvitacionesPlataforma invitaciones,
        IRepositorioCobrosPlataforma cobros,
        IReloj reloj)
    {
        _clubes = clubes;
        _invitaciones = invitaciones;
        _cobros = cobros;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClubResumenDto>> ListarAsync(CancellationToken cancelacion = default)
    {
        var filas = await _clubes.ListarAsync(cancelacion);
        var pendientes = await _cobros.ListarPendientesPorClubAsync(cancelacion);
        var hoy = ReglaMesDeCobro.DiaEnColombia(_reloj.AhoraUtc);

        return filas
            .Select(fila => MapperClubPlataforma.AResumen(
                fila,
                MapperCobroPlataforma.AEstadoDePago(
                    fila.Club, pendientes.GetValueOrDefault(fila.Club.Id) ?? [], hoy)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ClubDetalleDto> ObtenerDetalleAsync(Guid clubId, CancellationToken cancelacion = default)
    {
        var club = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var presidentes = await _clubes.ListarPresidentesAsync(clubId, cancelacion);
        var invitaciones = await _invitaciones.ListarPendientesAsync(clubId, cancelacion);
        var pendientes = await _cobros.ListarPendientesAsync(clubId, cancelacion);

        return MapperClubPlataforma.ADetalle(
            club,
            presidentes,
            invitaciones,
            _reloj.AhoraUtc,
            MapperCobroPlataforma.AEstadoDePago(club, pendientes, ReglaMesDeCobro.DiaEnColombia(_reloj.AhoraUtc)));
    }
}
