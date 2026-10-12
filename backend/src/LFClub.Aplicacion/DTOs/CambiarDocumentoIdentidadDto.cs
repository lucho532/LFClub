using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el cambio del documento de identidad de un jugador, por ejemplo al pasar de registro
/// civil a tarjeta de identidad (constitución §10).
/// Su responsabilidad es llevar el tipo y el número nuevos.
/// No lleva nombres, apellidos ni fecha de nacimiento: esos datos solo los corrige el PRESIDENTE,
/// en otra operación.
/// </summary>
/// <param name="TipoDocumento">Tipo de documento.</param>
/// <param name="NumeroDocumento">Número de documento, hasta 20 caracteres; se guarda sin espacios ni puntos.</param>
public record CambiarDocumentoIdentidadDto(TipoDocumento? TipoDocumento, string? NumeroDocumento);
