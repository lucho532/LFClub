namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el PRESIDENTE para fijar o quitar el valor propio de varios jugadores
/// (spec 015, RF-012).
/// Su responsabilidad es llevar los jugadores y el valor; sin valor, vuelven al del club.
/// No valida nada: lo hacen <c>ValidadorPago</c> y el servicio.
/// </summary>
/// <param name="UsuarioRolIds">Jugadores a los que se aplica; al menos uno.</param>
/// <param name="Valor">Valor propio en pesos enteros; nulo los vuelve al valor del club.</param>
public record FijarValorPropioDto(IReadOnlyList<Guid>? UsuarioRolIds, decimal? Valor);
