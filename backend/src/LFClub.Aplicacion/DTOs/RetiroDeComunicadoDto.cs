namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el retiro de un comunicado (spec 011, RF-026).
/// Su responsabilidad es llevar cuándo se retiró y el nombre copiado de quien lo retiró (§13).
/// No lo recibe nunca la cuenta de un jugador: lo retirado deja de existir para ella.
/// </summary>
/// <param name="En">Fecha y hora del retiro, en UTC.</param>
/// <param name="PorNombre">Nombres y apellidos de quien lo retiró.</param>
public record RetiroDeComunicadoDto(DateTime En, string PorNombre);
