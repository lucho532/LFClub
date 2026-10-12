namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la categoría a la que el PRESIDENTE pasa a un jugador (RF-014).
/// Su responsabilidad es llevar la categoría de destino.
/// No permite dejar al jugador sin categoría ni cambiar ningún otro dato suyo.
/// </summary>
/// <param name="CategoriaId">Categoría activa del club.</param>
public record UbicarJugadorDto(Guid? CategoriaId);
