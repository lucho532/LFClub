using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un club en la lista del panel de administración.
/// Su responsabilidad es mostrar su nombre, su estado, su identidad y si su presidente ya se registró.
/// Incluye cuántos integrantes activos tiene por rol, para la facturación; no contiene datos personales
/// de ningún integrante ni información interna del club.
/// </summary>
/// <param name="ClubId">Identificador del club.</param>
/// <param name="Nombre">Nombre del club.</param>
/// <param name="Estado">Estado actual.</param>
/// <param name="PresidenteRegistrado">Verdadero si el club tiene algún integrante con rol PRESIDENTE.</param>
/// <param name="Identidad">Identidad visual del club.</param>
/// <param name="Integrantes">Integrantes activos del club contados por rol.</param>
public record ClubResumenDto(
    Guid ClubId,
    string Nombre,
    EstadoClub Estado,
    bool PresidenteRegistrado,
    IdentidadClubDto Identidad,
    ConteoIntegrantesDto Integrantes);
