using System.Text.Json.Serialization;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la carta de un solo jugador tal como la recibe quien ve su ficha (spec 010, RF-019 a
/// RF-028).
/// Su responsabilidad es llevar su nombre, si tiene foto, su dorsal, su posición, sus seis
/// estadísticas con la valoración, la rareza por asistencia, lo que quien pregunta puede hacer con
/// ella y, solo a quien puede cambiar las estadísticas, su último cambio.
/// No lleva el escudo ni los colores, que son los de la identidad del club (spec 007); ni el
/// porcentaje de asistencia, la categoría o datos personales (supuesto 4, RF-030). Lo que no hay se
/// omite del JSON.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="FotoActualizadaEn">Última carga de su foto; ausente si no tiene (figura genérica).</param>
/// <param name="Dorsal">Dorsal; ausente si no tiene.</param>
/// <param name="Posicion">Posición; ausente si no tiene.</param>
/// <param name="Estadisticas">Las seis; ausentes si no tiene.</param>
/// <param name="Valoracion">Promedio redondeado; ausente sin estadísticas.</param>
/// <param name="Rareza">Bronce, Plata u Oro por la asistencia de la temporada.</param>
/// <param name="Permisos">Lo que quien pregunta puede hacer con ella.</param>
/// <param name="UltimoCambioEstadisticas">Quién y cuándo cambió las estadísticas; solo si puede cambiarlas (RF-028).</param>
public record CartaDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? FotoActualizadaEn,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Dorsal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PosicionJugador? Posicion,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ValoresCartaDto? Estadisticas,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Valoracion,
    RarezaCarta Rareza,
    PermisosDeCartaDto Permisos,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] UltimoCambioDto? UltimoCambioEstadisticas);
