namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el DESARROLLADOR para anular un cobro pendiente (spec 017).
/// Su responsabilidad es llevar el motivo, obligatorio, de 3 a 300 caracteres.
/// No valida nada: lo hace <c>ServicioCobrosDeUnClub</c>.
/// </summary>
/// <param name="Motivo">Por qué se anula.</param>
public record AnularCobroDto(string? Motivo);
