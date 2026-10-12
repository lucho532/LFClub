namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el contacto que muestra la ficha de un jugador.
/// Su responsabilidad es llevar el correo, el celular y el nombre del responsable, que son datos
/// de la cuenta: los mismos en todos sus jugadores y en todos sus clubes (RF-039).
/// No lleva el contacto de emergencia, que es de la ficha. El correo se muestra, pero no se cambia
/// desde la ficha (RF-018).
/// </summary>
/// <param name="Correo">Correo de la cuenta.</param>
/// <param name="Celular">Celular de contacto.</param>
/// <param name="NombreResponsable">Nombre del padre, madre o responsable.</param>
public record ContactoDeFichaDto(string Correo, string? Celular, string? NombreResponsable);
