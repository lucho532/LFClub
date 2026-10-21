namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un pago que espera revisión, con el jugador y sus cargos pendientes (spec 015, RF-022).
/// Su responsabilidad es darle al PRESIDENTE lo que necesita para confirmarlo: el pago, quién es el
/// jugador y qué debe.
/// No lleva los bytes del soporte, que se piden aparte.
/// </summary>
/// <param name="Pago">El pago informado.</param>
/// <param name="JugadorNombre">Nombre del jugador.</param>
/// <param name="CargosPendientes">Cargos del jugador con algo pendiente, del más antiguo al más reciente.</param>
public record PagoEnRevisionDto(PagoDto Pago, string JugadorNombre, IReadOnlyList<CargoDto> CargosPendientes);
