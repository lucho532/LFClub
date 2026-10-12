namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la seguridad social de un jugador.
/// Su responsabilidad es llevar la entidad de salud a la que está afiliado y el lugar donde lo
/// atienden, los dos opcionales.
/// No lleva datos clínicos ni el certificado de afiliación, que es un documento de la ficha.
/// </summary>
/// <param name="EntidadSalud">Entidad de salud a la que está afiliado.</param>
/// <param name="LugarAtencion">Lugar donde lo atienden.</param>
public record SeguridadSocialDto(string? EntidadSalud, string? LugarAtencion);
