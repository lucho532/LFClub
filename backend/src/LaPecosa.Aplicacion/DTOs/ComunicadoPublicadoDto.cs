using System.Text.Json.Serialization;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa un comunicado como lo ve el equipo del club (spec 011, RF-024 a RF-026).
/// Su responsabilidad es llevar el título, el texto, quién lo publicó, la audiencia con sus
/// destinos, a cuántas familias llegó, si está retirado y si quien pregunta puede retirarlo.
/// No lo recibe nunca la cuenta de un jugador (RF-032): <see cref="Retirado"/> se omite del JSON
/// mientras está publicado.
/// </summary>
/// <param name="Id">Identificador del comunicado.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Texto">Texto plano.</param>
/// <param name="PublicadoEn">Fecha y hora de publicación, en UTC.</param>
/// <param name="PublicadoPor">Quién lo publicó.</param>
/// <param name="Alcance">A quién iba dirigido.</param>
/// <param name="Destinos">Categorías o equipos destino; vacío con <c>CLUB</c>.</param>
/// <param name="FamiliasAlcanzadas">Cuentas que lo recibieron al publicar.</param>
/// <param name="Retirado">El retiro, si está retirado.</param>
/// <param name="PuedeRetirar">Falso si ya está retirado o si no es suyo ni es PRESIDENTE.</param>
public record ComunicadoPublicadoDto(
    Guid Id,
    string Titulo,
    string Texto,
    DateTime PublicadoEn,
    AutorDeComunicadoDto PublicadoPor,
    AlcanceComunicado Alcance,
    IReadOnlyList<DestinoDeComunicadoDto> Destinos,
    int FamiliasAlcanzadas,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RetiroDeComunicadoDto? Retirado,
    bool PuedeRetirar);
