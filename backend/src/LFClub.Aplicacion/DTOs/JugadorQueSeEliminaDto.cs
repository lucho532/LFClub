namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador que se eliminaría junto con la cuenta (spec 014, RF-003).
/// Su responsabilidad es identificarlo ante quien confirma: nombre completo, club y si está retirado
/// o en espera, para decirlo en texto.
/// No lleva documento, fecha de nacimiento ni ningún otro dato de la ficha.
/// </summary>
/// <param name="NombreCompleto">Nombres y apellidos.</param>
/// <param name="ClubNombre">Nombre del club al que pertenece.</param>
/// <param name="Retirado">Verdadero si está retirado del club.</param>
/// <param name="EnEspera">Verdadero si su ingreso aún espera aprobación.</param>
public record JugadorQueSeEliminaDto(string NombreCompleto, string ClubNombre, bool Retirado, bool EnEspera);
