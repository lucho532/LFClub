using System.Text.Json.Serialization;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa un aviso de actividad como lo ve la cuenta de un jugador (spec 012, RF-012 a RF-017).
/// Su responsabilidad es llevar la línea ya redactada, cuándo se generó, la actividad a la que
/// apunta y si la cuenta ya lo leyó. <see cref="LeidoEn"/> se omite del JSON mientras no se ha leído.
/// No lleva autor, título ni audiencia: el aviso lo escribe el sistema.
/// </summary>
/// <param name="Id">Identificador del aviso.</param>
/// <param name="Texto">Texto plano de 300 caracteres como máximo; nunca se interpreta.</param>
/// <param name="GeneradoEn">Fecha y hora en que se generó, en UTC.</param>
/// <param name="Actividad">Actividad a la que se refiere.</param>
/// <param name="Leido">Si la cuenta ya lo leyó.</param>
/// <param name="LeidoEn">Cuándo lo leyó la cuenta, en UTC; ausente si no lo ha leído.</param>
public record AvisoRecibidoDto(
    Guid Id,
    string Texto,
    DateTime GeneradoEn,
    ActividadDeAvisoDto Actividad,
    bool Leido,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? LeidoEn);
