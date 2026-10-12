namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos con los que una cuenta que ya existe acepta una invitación (RF-010).
/// Su responsabilidad es llevar el token del enlace y, solo cuando hace falta, el nombre del
/// responsable que la cuenta no tiene.
/// No lleva correo, club, rol ni ningún otro dato de la persona: salen de la invitación y de la
/// cuenta.
/// </summary>
/// <param name="Token">Token del enlace de invitación.</param>
/// <param name="NombreResponsable">
/// Nombre del padre, madre o responsable, hasta 160 caracteres. Obligatorio solo cuando la
/// invitación es de JUGADOR, la persona es menor de 18 años ese día y su cuenta no tiene
/// responsable. En cualquier otro caso se ignora y no se guarda (RF-026).
/// </param>
public record AceptarInvitacionDto(
    string? Token,
    string? NombreResponsable);
