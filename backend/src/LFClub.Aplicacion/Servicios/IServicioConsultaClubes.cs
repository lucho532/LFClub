using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa las consultas del panel de administración sobre los clubes.
/// Su responsabilidad es listar todos los clubes y mostrar el detalle de uno, con sus presidentes
/// y sus invitaciones sin usar.
/// No modifica nada y no expone datos internos de ningún club (constitución §8).
/// </summary>
public interface IServicioConsultaClubes
{
    /// <summary>Lista todos los clubes con su estado.</summary>
    Task<IReadOnlyList<ClubResumenDto>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Detalle de un club; 404 <c>no_encontrado</c> si no existe.</summary>
    Task<ClubDetalleDto> ObtenerDetalleAsync(Guid clubId, CancellationToken cancelacion = default);
}
