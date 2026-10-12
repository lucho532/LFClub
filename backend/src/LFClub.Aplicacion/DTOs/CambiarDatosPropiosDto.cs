namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que un PRESIDENTE, DIRECTIVO o ENTRENADOR cambia de sí mismo desde "Mi perfil"
/// (spec 008, RF-016).
/// Su responsabilidad es llevar los cuatro datos que puede cambiar: nombres, apellidos, fecha de
/// nacimiento y celular. Llegan opcionales para que el validador diga qué falta en cada campo.
/// No tiene documento ni correo: si vienen en el cuerpo, la deserialización los ignora y no
/// cambian (RF-018, RF-019).
/// </summary>
/// <param name="Nombres">Nombres; obligatorios, hasta 80 caracteres.</param>
/// <param name="Apellidos">Apellidos; obligatorios, hasta 80 caracteres.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento; obligatoria y no futura.</param>
/// <param name="Celular">Celular de la cuenta; obligatorio, hasta 20 caracteres.</param>
public record CambiarDatosPropiosDto(string? Nombres, string? Apellidos, DateOnly? FechaNacimiento, string? Celular);
