namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa la convocatoria completa que se guarda para un partido (spec 009, RF-009 y RF-013).
/// Su responsabilidad es llevar todos los jugadores que deben quedar convocados; puede ir vacía.
/// No dice quién entra ni quién sale: eso lo calcula el servicio.
/// </summary>
/// <param name="UsuarioRolIds">Los jugadores convocados, sin repetir.</param>
public record GuardarConvocatoriaDto(IReadOnlyList<Guid>? UsuarioRolIds);
