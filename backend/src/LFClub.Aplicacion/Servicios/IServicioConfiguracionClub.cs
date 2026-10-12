using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de editar los datos de un club: nombre, sede, dirección y contacto
/// (constitución §7.2, RF-010).
/// Su responsabilidad es aplicar el mismo cambio y las mismas validaciones tanto si lo hace el
/// PRESIDENTE sobre su propio club como si lo hace el DESARROLLADOR desde su panel.
/// No cambia el escudo, los colores ni el estado del club.
/// </summary>
public interface IServicioConfiguracionClub
{
    /// <summary>
    /// Edita el club de la petición, para su presidente. Nombre repetido: 409
    /// <c>nombre_de_club_repetido</c>.
    /// </summary>
    Task<ClubDto> ActualizarElPropioAsync(
        ActualizarConfiguracionClubDto datos, UsuarioRol quienEdita, CancellationToken cancelacion = default);

    /// <summary>Edita cualquier club, para el DESARROLLADOR. Club inexistente: 404.</summary>
    Task<ClubDetalleDto> ActualizarDesdeElPanelAsync(
        Guid clubId, ActualizarConfiguracionClubDto datos, CancellationToken cancelacion = default);
}
