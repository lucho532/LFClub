namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la referencia a un equipo dentro de un jugador o de un entrenador.
/// Su responsabilidad es identificarlo y nombrarlo.
/// No lleva sus jugadores ni sus entrenadores.
/// </summary>
/// <param name="EquipoId">Identificador del equipo.</param>
/// <param name="Nombre">Nombre del equipo.</param>
public record EquipoDeReferenciaDto(Guid EquipoId, string Nombre);
