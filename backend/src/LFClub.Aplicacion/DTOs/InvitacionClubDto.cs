using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una invitación tal como la ve el club que la envió (RF-003 y RF-019).
/// Su responsabilidad es mostrar a quién se envió y con qué rol, en qué estado está, si el correo
/// salió, quién la envió y cuándo vence. Una invitación usada es el registro de que esa persona
/// entró al club, con qué rol y quién la invitó.
/// Nunca incluye el token ni su hash.
/// </summary>
/// <param name="InvitacionId">Identificador de la invitación.</param>
/// <param name="Correo">Correo invitado.</param>
/// <param name="Rol">Rol con el que entra quien la use: JUGADOR, ENTRENADOR o DIRECTIVO.</param>
/// <param name="Estado">Pendiente, usada, vencida o cancelada.</param>
/// <param name="EstadoEnvio">Resultado del envío del correo.</param>
/// <param name="EnviadaPor">Nombre de quien la envió; nulo si ya no está en el club.</param>
/// <param name="CreadaEn">Fecha de envío, en UTC.</param>
/// <param name="VenceEn">Fecha de vencimiento, en UTC.</param>
public record InvitacionClubDto(
    Guid InvitacionId,
    string Correo,
    Rol Rol,
    EstadoInvitacion Estado,
    EstadoEnvio EstadoEnvio,
    string? EnviadaPor,
    DateTime CreadaEn,
    DateTime VenceEn);
