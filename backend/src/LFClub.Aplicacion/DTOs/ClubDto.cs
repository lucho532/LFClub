using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que ve un integrante de su club.
/// Su responsabilidad es llevar la configuración y la identidad del club elegido, y el rol y el
/// identificador de integrante de quien lo consulta.
/// No contiene datos de otros integrantes ni información que solo ve el DESARROLLADOR.
/// </summary>
/// <param name="ClubId">Identificador del club.</param>
/// <param name="Nombre">Nombre del club.</param>
/// <param name="Sede">Sede.</param>
/// <param name="Direccion">Dirección.</param>
/// <param name="CorreoContacto">Correo de contacto.</param>
/// <param name="TelefonoContacto">Teléfono de contacto.</param>
/// <param name="Estado">Estado actual.</param>
/// <param name="Identidad">Identidad visual del club.</param>
/// <param name="MiRol">Rol de quien consulta en este club.</param>
/// <param name="MiUsuarioRolId">
/// Identificador del integrante de quien consulta en este club. Con el rol JUGADOR es el del
/// jugador cuya ficha abre "Mi ficha".
/// </param>
public record ClubDto(
    Guid ClubId,
    string Nombre,
    string? Sede,
    string? Direccion,
    string? CorreoContacto,
    string? TelefonoContacto,
    EstadoClub Estado,
    IdentidadClubDto Identidad,
    Rol MiRol,
    Guid MiUsuarioRolId);
