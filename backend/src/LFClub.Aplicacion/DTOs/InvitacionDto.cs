using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una invitación tal como la ve el DESARROLLADOR.
/// Su responsabilidad es mostrar a quién se envió, si el correo salió y si ya venció.
/// Nunca incluye el token ni su hash.
/// </summary>
/// <param name="InvitacionId">Identificador de la invitación.</param>
/// <param name="Correo">Correo invitado.</param>
/// <param name="Rol">Rol con el que entra quien la use.</param>
/// <param name="EstadoEnvio">Resultado del envío del correo.</param>
/// <param name="CreadaEn">Fecha de creación, en UTC.</param>
/// <param name="VenceEn">Fecha de vencimiento, en UTC.</param>
/// <param name="Vencida">Verdadero si ya pasó su vencimiento.</param>
public record InvitacionDto(
    Guid InvitacionId,
    string Correo,
    Rol Rol,
    EstadoEnvio EstadoEnvio,
    DateTime CreadaEn,
    DateTime VenceEn,
    bool Vencida);
