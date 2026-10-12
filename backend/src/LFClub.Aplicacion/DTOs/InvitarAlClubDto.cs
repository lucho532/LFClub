using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos para invitar a una persona a registrarse en el club (RF-001 y RF-002).
/// Su responsabilidad es llevar el correo de la persona invitada y el rol con el que entra.
/// No lleva el club, que es el de la ruta. Admite cualquier valor de la enumeración de roles para
/// que uno no invitable (PRESIDENTE, por ejemplo) se rechace con su propio error en lugar de como
/// un dato mal escrito.
/// </summary>
/// <param name="Correo">Correo al que se envía la invitación.</param>
/// <param name="Rol">Rol con el que entra la persona: JUGADOR, ENTRENADOR o DIRECTIVO. Obligatorio.</param>
public record InvitarAlClubDto(string? Correo, Rol? Rol);
