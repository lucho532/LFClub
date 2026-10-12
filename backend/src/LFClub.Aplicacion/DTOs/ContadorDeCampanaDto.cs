namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el número que muestra la campana (spec 012, RF-018).
/// Su responsabilidad es llevar cuántos elementos distintos, entre comunicados y avisos, tiene la
/// cuenta sin leer, exacto; la pantalla escribe "99+" desde 100.
/// No lleva la lista.
/// </summary>
/// <param name="SinLeer">Comunicados más avisos sin leer.</param>
public record ContadorDeCampanaDto(int SinLeer);
