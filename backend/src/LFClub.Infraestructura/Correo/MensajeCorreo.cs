namespace LFClub.Infraestructura.Correo;

/// <summary>
/// Representa un correo ya redactado.
/// Su responsabilidad es llevar el asunto, el texto y el enlace que contiene.
/// No sabe a quién se envía ni cómo.
/// </summary>
/// <param name="Asunto">Asunto del correo.</param>
/// <param name="Texto">Cuerpo en texto plano, con el enlace incluido.</param>
/// <param name="Enlace">Enlace de un solo uso que lleva el correo.</param>
public record MensajeCorreo(string Asunto, string Texto, string Enlace);
