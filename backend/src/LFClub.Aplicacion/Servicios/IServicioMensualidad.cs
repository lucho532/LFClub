using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de definir cuánto cuesta la mensualidad (spec 015, RF-011 y RF-012).
/// Su responsabilidad es consultar el valor del club y los valores propios, fijar el valor del club
/// y fijar o quitar el valor propio de uno o varios jugadores.
/// No toca los cargos ya generados (RF-013) ni comprueba el rol: lo hace la autorización del
/// controlador.
/// </summary>
public interface IServicioMensualidad
{
    /// <summary>El valor del club (nulo si no lo definió) y los jugadores con valor propio.</summary>
    Task<MensualidadDto> ObtenerAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Fija el valor de la mensualidad del club y genera en el acto los cargos del mes en curso. Valor
    /// no entero, cero o negativo: 400.
    /// </summary>
    Task<MensualidadDto> FijarValorDelClubAsync(FijarValorMensualidadDto datos, CancellationToken cancelacion = default);

    /// <summary>
    /// Fija el valor propio de los jugadores, o lo quita con valor nulo, todo o nada. Si alguno no es un
    /// jugador activo del club: 404 y no cambia ninguno.
    /// </summary>
    Task<MensualidadDto> FijarValorPropioAsync(FijarValorPropioDto datos, CancellationToken cancelacion = default);
}
