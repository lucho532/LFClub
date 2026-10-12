using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la decisión del DESARROLLADOR al quitar el rol a un presidente (constitución §8).
/// Su responsabilidad es obligar a elegir entre asignarle otro rol en el club o eliminarlo del club.
/// No permite dejar al integrante sin rol ni asignarle un rol distinto de DIRECTIVO o ENTRENADOR.
/// </summary>
/// <param name="Accion">Qué hacer con el presidente. Obligatorio.</param>
/// <param name="RolNuevo">Rol nuevo, DIRECTIVO o ENTRENADOR; obligatorio al asignar otro rol.</param>
public record RetirarPresidenteDto(
    AccionRetiroPresidente? Accion,
    Rol? RolNuevo);
