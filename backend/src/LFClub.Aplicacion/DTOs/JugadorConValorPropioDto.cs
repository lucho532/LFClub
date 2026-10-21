namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador al que el PRESIDENTE le fijó un valor de mensualidad propio (spec 015,
/// RF-012).
/// Su responsabilidad es llevar quién es y cuánto paga.
/// No lleva su documento ni su estado de cuenta.
/// </summary>
/// <param name="UsuarioRolId">Identificador del jugador en el club.</param>
/// <param name="NombreCompleto">Nombres y apellidos.</param>
/// <param name="Valor">Valor propio, en pesos enteros.</param>
public record JugadorConValorPropioDto(Guid UsuarioRolId, string NombreCompleto, decimal Valor);
