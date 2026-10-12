namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos para iniciar sesión.
/// Su responsabilidad es llevar el identificador (correo o documento) y la contraseña.
/// No se normaliza aquí: lo hace el servicio, y la contraseña nunca se modifica.
/// </summary>
/// <param name="Identificador">Correo o número de documento.</param>
/// <param name="Contrasena">Contraseña, tal como se escribió.</param>
public record IniciarSesionDto(
    string? Identificador,
    string? Contrasena);
