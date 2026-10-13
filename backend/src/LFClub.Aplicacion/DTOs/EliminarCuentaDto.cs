namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el cuerpo de la petición para eliminar la propia cuenta (spec 014, RF-002).
/// Su responsabilidad es llevar la contraseña con la que la persona confirma que es ella. Llega
/// opcional para que el servicio responda 400 <c>datos_invalidos</c> si falta.
/// No lleva identificador de cuenta ni de club: siempre se elimina la cuenta de la sesión.
/// </summary>
/// <param name="Contrasena">La contraseña de la cuenta; obligatoria.</param>
public record EliminarCuentaDto(string? Contrasena);
