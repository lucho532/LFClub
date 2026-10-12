namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa a toda entidad cuyos datos pertenecen a exactamente un club (constitución §7.1).
/// Su responsabilidad es exponer el club dueño, para que el filtro de aislamiento se aplique solo.
/// No la implementan las entidades de la plataforma (club, cuenta, foto y recuperación).
/// </summary>
public interface IPerteneceAClub
{
    /// <summary>Club al que pertenece el dato.</summary>
    Guid ClubId { get; }
}
