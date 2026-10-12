using System.Text.Json.Serialization;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un comunicado como lo ve la cuenta de un jugador (spec 011, RF-012 a RF-016).
/// Su responsabilidad es llevar el título, el texto, quién lo publicó y si la cuenta ya lo leyó.
/// No lleva la audiencia, los destinos ni a cuántas familias llegó (RF-032): esas partes existen
/// solo en <see cref="ComunicadoPublicadoDto"/>. <see cref="LeidoEn"/> se omite del JSON mientras
/// no se ha leído.
/// </summary>
/// <param name="Id">Identificador del comunicado.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Texto">Texto plano.</param>
/// <param name="PublicadoEn">Fecha y hora de publicación, en UTC.</param>
/// <param name="PublicadoPor">Quién lo publicó.</param>
/// <param name="Leido">Si la cuenta ya lo leyó.</param>
/// <param name="LeidoEn">Cuándo lo leyó la cuenta, en UTC; ausente si no lo ha leído.</param>
public record ComunicadoRecibidoDto(
    Guid Id,
    string Titulo,
    string Texto,
    DateTime PublicadoEn,
    AutorDeComunicadoDto PublicadoPor,
    bool Leido,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? LeidoEn);
