namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un equipo activo de una categoría.
/// Su responsabilidad es llevar su nombre, cuántos jugadores tiene y si todavía se puede borrar.
/// No existe para un equipo desactivado, que no aparece en ninguna respuesta.
/// </summary>
/// <param name="EquipoId">Identificador del equipo.</param>
/// <param name="Nombre">Nombre del equipo.</param>
/// <param name="NumeroJugadores">Jugadores que están hoy en el equipo.</param>
/// <param name="SePuedeBorrar">Verdadero si nunca ha tenido jugadores ni entrenadores.</param>
public record EquipoDto(Guid EquipoId, string Nombre, int NumeroJugadores, bool SePuedeBorrar);
