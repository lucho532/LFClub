using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de consultar la carta de un jugador (spec 010, historia 3).
/// Su responsabilidad es que la vean exactamente quienes ven su ficha, con su valoración y su rareza
/// calculadas en el momento.
/// No conoce HTTP ni cambia nada.
/// </summary>
public interface IServicioConsultaCarta
{
    /// <summary>La carta del jugador, como la ve quien pregunta.</summary>
    Task<CartaDto> ObtenerAsync(Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>La rareza del jugador por su asistencia a los entrenamientos de la temporada.</summary>
    Task<RarezaCarta> RarezaAsync(Guid usuarioRolId, CancellationToken cancelacion = default);
}
