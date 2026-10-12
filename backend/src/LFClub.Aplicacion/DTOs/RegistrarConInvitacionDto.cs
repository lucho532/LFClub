using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos del registro con una invitación (constitución §12.1).
/// Su responsabilidad es llevar la identidad de la persona, sus datos de contacto y la contraseña
/// que ella misma crea.
/// No lleva correo, club ni rol: los tres salen siempre de la invitación.
/// </summary>
/// <param name="Token">Token del enlace de invitación.</param>
/// <param name="Nombres">Nombres, hasta 80 caracteres.</param>
/// <param name="Apellidos">Apellidos, hasta 80 caracteres.</param>
/// <param name="TipoDocumento">Tipo de documento.</param>
/// <param name="NumeroDocumento">Número de documento, hasta 20 caracteres.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento; no puede ser futura.</param>
/// <param name="Celular">Celular de contacto, hasta 20 caracteres.</param>
/// <param name="NombreResponsable">
/// Nombre del padre, madre o responsable, hasta 160 caracteres. Solo se usa con una invitación de
/// JUGADOR: obligatorio si la persona es menor de 18 años el día del registro y opcional si es
/// adulta. Con cualquier otro rol se ignora y no se guarda (RF-013).
/// </param>
/// <param name="Contrasena">Contraseña, de 8 a 128 caracteres.</param>
public record RegistrarConInvitacionDto(
    string? Token,
    string? Nombres,
    string? Apellidos,
    TipoDocumento? TipoDocumento,
    string? NumeroDocumento,
    DateOnly? FechaNacimiento,
    string? Celular,
    string? NombreResponsable,
    string? Contrasena);
