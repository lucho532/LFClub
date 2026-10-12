using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de consultas del panel de administración.
/// Su responsabilidad es reunir el club, sus presidentes y sus invitaciones pendientes y
/// convertirlos en DTO.
/// No modifica datos, no accede al contexto de Entity Framework y no decide quién puede consultar.
/// </summary>
public class ServicioConsultaClubes : IServicioConsultaClubes
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioInvitacionesPlataforma _invitaciones;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaClubes(
        IRepositorioClubesPlataforma clubes, IRepositorioInvitacionesPlataforma invitaciones, IReloj reloj)
    {
        _clubes = clubes;
        _invitaciones = invitaciones;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClubResumenDto>> ListarAsync(CancellationToken cancelacion = default) =>
        (await _clubes.ListarAsync(cancelacion)).Select(MapperClubPlataforma.AResumen).ToList();

    /// <inheritdoc />
    public async Task<ClubDetalleDto> ObtenerDetalleAsync(Guid clubId, CancellationToken cancelacion = default)
    {
        var club = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var presidentes = await _clubes.ListarPresidentesAsync(clubId, cancelacion);
        var invitaciones = await _invitaciones.ListarPendientesAsync(clubId, cancelacion);

        return MapperClubPlataforma.ADetalle(club, presidentes, invitaciones, _reloj.AhoraUtc);
    }
}
