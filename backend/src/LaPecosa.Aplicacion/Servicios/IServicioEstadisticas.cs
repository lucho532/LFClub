using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de las estadísticas deportivas: las de un jugador en su ficha y los
/// goleadores del club por categoría (spec 009, historia 6).
/// Su responsabilidad es responder con cifras calculadas de lo registrado, solo a quien puede ver la
/// ficha del jugador, y con los goleadores a cualquier integrante del club.
/// No modifica datos ni conoce HTTP.
/// </summary>
public interface IServicioEstadisticas
{
    /// <summary>Las estadísticas de un jugador; el mismo 404 de la ficha a quien no puede verla.</summary>
    Task<EstadisticasJugadorDto> DelJugadorAsync(Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Las categorías con goles y, si se elige una, sus goleadores.</summary>
    Task<GoleadoresDto> GoleadoresAsync(Guid? categoriaId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
