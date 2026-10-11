using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa a un integrante del club junto con la versión de la foto de perfil de su cuenta.
/// Su responsabilidad es evitar una segunda consulta para saber si la persona tiene foto.
/// No contiene la foto ni ningún otro dato de la cuenta.
/// </summary>
/// <param name="Integrante">El integrante del club.</param>
/// <param name="VersionFoto">Versión de la foto de perfil de su cuenta; 0 si no tiene.</param>
public record PersonaConFoto(UsuarioRol Integrante, int VersionFoto);
