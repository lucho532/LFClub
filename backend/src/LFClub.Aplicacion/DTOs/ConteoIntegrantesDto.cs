namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa cuántos integrantes activos tiene un club por rol.
/// Su responsabilidad es dar al panel del DESARROLLADOR los números para la facturación.
/// No identifica a ningún integrante. Cuenta solo ingresos aprobados y no retirados.
/// </summary>
/// <param name="Jugadores">Integrantes con rol JUGADOR.</param>
/// <param name="Entrenadores">Integrantes con rol ENTRENADOR.</param>
/// <param name="Directivos">Integrantes con rol DIRECTIVO (los coordinadores del club).</param>
/// <param name="Presidentes">Integrantes con rol PRESIDENTE.</param>
public record ConteoIntegrantesDto(int Jugadores, int Entrenadores, int Directivos, int Presidentes);
