using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos clínicos de un jugador: los más restringidos de su ficha.
/// Su responsabilidad es llevar el grupo sanguíneo, las alergias, las enfermedades o condiciones,
/// los medicamentos y las observaciones, todos opcionales.
/// No se entrega nunca a un DIRECTIVO, tampoco si entrena la categoría del jugador (RF-010): para
/// él la ficha no trae este grupo.
/// </summary>
/// <param name="GrupoSanguineo">Grupo sanguíneo.</param>
/// <param name="Alergias">Alergias.</param>
/// <param name="Enfermedades">Enfermedades o condiciones.</param>
/// <param name="Medicamentos">Medicamentos.</param>
/// <param name="Observaciones">Observaciones.</param>
public record DatosClinicosDto(
    GrupoSanguineo? GrupoSanguineo,
    string? Alergias,
    string? Enfermedades,
    string? Medicamentos,
    string? Observaciones);
