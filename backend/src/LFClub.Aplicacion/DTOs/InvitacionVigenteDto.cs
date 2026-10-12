using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos fijos de una invitación vigente, para mostrar el registro.
/// Su responsabilidad es decir a qué club, con qué rol y a qué correo invita, si ese correo ya
/// tiene cuenta y si el registro, o la aceptación, debe pedir el nombre del responsable. La
/// pantalla muestra siempre el rol, que no se puede cambiar (RF-006).
/// No contiene el token ni ningún dato interno del club.
/// </summary>
/// <param name="NombreClub">Nombre del club que invita.</param>
/// <param name="Rol">Rol con el que entra quien la use.</param>
/// <param name="Correo">Correo invitado; no se puede cambiar.</param>
/// <param name="TieneCuenta">Si es verdadero, la persona debe iniciar sesión y aceptar la invitación.</param>
/// <param name="PideResponsable">
/// Verdadero solo en las invitaciones de JUGADOR: el registro pide entonces el nombre del padre,
/// madre o responsable (RF-013).
/// </param>
/// <param name="FaltaResponsable">
/// Verdadero solo cuando el correo ya tiene cuenta, la invitación es de JUGADOR, la persona es
/// menor de 18 años y su cuenta no tiene responsable: la aceptación pide entonces ese nombre
/// (RF-026).
/// </param>
/// <param name="Identidad">Identidad visual del club.</param>
public record InvitacionVigenteDto(
    string NombreClub,
    Rol Rol,
    string Correo,
    bool TieneCuenta,
    bool PideResponsable,
    bool FaltaResponsable,
    IdentidadClubDto Identidad);
