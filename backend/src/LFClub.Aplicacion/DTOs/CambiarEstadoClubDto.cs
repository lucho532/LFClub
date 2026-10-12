using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el cambio de estado de un club que pide el DESARROLLADOR (constitución §7.4).
/// Su responsabilidad es llevar el estado al que debe pasar el club.
/// No sirve para eliminar un club: eso es otra operación, con su propia confirmación.
/// </summary>
/// <param name="Estado">Estado al que pasa el club.</param>
public record CambiarEstadoClubDto(
    EstadoClub? Estado);
