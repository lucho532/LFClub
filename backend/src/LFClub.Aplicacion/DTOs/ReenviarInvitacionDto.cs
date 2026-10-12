namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el reenvío de una invitación sin usar.
/// Su responsabilidad es llevar el correo corregido, si se corrige.
/// Si el correo falta, se reenvía al mismo.
/// </summary>
/// <param name="Correo">Correo corregido, o nulo para reenviar al mismo.</param>
public record ReenviarInvitacionDto(
    string? Correo);
