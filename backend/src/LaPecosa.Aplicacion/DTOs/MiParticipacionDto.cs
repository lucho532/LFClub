using System.Text.Json.Serialization;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa lo que la familia de un convocado ve de su propio jugador en un partido que ya empezó
/// (spec 010, RF-004 y RF-012).
/// Su responsabilidad es decir si jugó, sus goles, quién lo indicó y si la familia todavía puede
/// cambiarlo.
/// Nunca lleva datos de otro convocado ni la suma de goles de los demás (RF-005).
/// </summary>
/// <param name="Jugo">Si jugó.</param>
/// <param name="Goles">Sus goles.</param>
/// <param name="IndicadoPor">Quién lo indicó; ausente si nadie indicó nada.</param>
/// <param name="PuedeCambiar">Falso si quien entrena o el PRESIDENTE ya lo corrigió.</param>
public record MiParticipacionDto(
    bool Jugo,
    int Goles,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] OrigenResultado? IndicadoPor,
    bool PuedeCambiar);
