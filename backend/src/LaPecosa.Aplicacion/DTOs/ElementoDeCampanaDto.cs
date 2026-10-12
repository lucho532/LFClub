using System.Text.Json.Serialization;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa las dos clases de elemento que mezcla la campana (spec 012, RF-018).
/// Su responsabilidad es indicar cuál de los dos campos de <see cref="ElementoDeCampanaDto"/> trae
/// el contenido.
/// No lleva datos.
/// </summary>
public enum TipoDeElementoDeCampana
{
    /// <summary>Un comunicado del equipo del club (spec 011).</summary>
    COMUNICADO,

    /// <summary>Un aviso de actividad escrito por el sistema (spec 012).</summary>
    AVISO,
}

/// <summary>
/// Representa un elemento de la campana de una cuenta (spec 012, RF-018 a RF-020).
/// Su responsabilidad es llevar exactamente uno de los dos contenidos, el que dice
/// <see cref="Tipo"/>; el otro se omite del JSON.
/// No ordena la lista: el orden es el de la página que lo contiene.
/// </summary>
/// <param name="Tipo">Qué trae este elemento.</param>
/// <param name="Comunicado">El comunicado, con <c>COMUNICADO</c>.</param>
/// <param name="Aviso">El aviso, con <c>AVISO</c>.</param>
public record ElementoDeCampanaDto(
    TipoDeElementoDeCampana Tipo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ComunicadoRecibidoDto? Comunicado,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] AvisoRecibidoDto? Aviso);
