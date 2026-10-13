namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un club que impide eliminar la cuenta porque la persona es su único PRESIDENTE
/// (spec 014, RF-009).
/// Su responsabilidad es nombrar el club para decirle a la persona dónde nombrar a otro presidente.
/// No lleva ningún otro dato del club.
/// </summary>
/// <param name="ClubId">Identificador del club.</param>
/// <param name="ClubNombre">Nombre del club.</param>
public record ClubQueImpideDto(Guid ClubId, string ClubNombre);
