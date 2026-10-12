using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un club en la lista del panel de administración.
/// Su responsabilidad es mostrar su nombre, su estado, su identidad y si su presidente ya se registró.
/// No contiene datos de integrantes ni información interna del club.
/// </summary>
/// <param name="ClubId">Identificador del club.</param>
/// <param name="Nombre">Nombre del club.</param>
/// <param name="Estado">Estado actual.</param>
/// <param name="PresidenteRegistrado">Verdadero si el club tiene algún integrante con rol PRESIDENTE.</param>
/// <param name="Identidad">Identidad visual del club.</param>
public record ClubResumenDto(
    Guid ClubId,
    string Nombre,
    EstadoClub Estado,
    bool PresidenteRegistrado,
    IdentidadClubDto Identidad);
