namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía la familia de un convocado sobre un partido que ya empezó (spec 010,
/// RF-001 a RF-004).
/// Su responsabilidad es llevar, solo si el partido todavía no lo tiene, el marcador, y siempre lo
/// del jugador de la petición: si jugó y sus goles.
/// No lleva datos de otros convocados ni valida nada: lo hacen el servicio y
/// <c>ReglaResultadoDeFamilia</c>.
/// </summary>
/// <param name="GolesClub">Goles del club, de 0 a 99; solo si el partido no tiene marcador.</param>
/// <param name="GolesRival">Goles del rival, de 0 a 99; solo si el partido no tiene marcador.</param>
/// <param name="Jugo">Si su jugador jugó.</param>
/// <param name="Goles">Goles de su jugador; 0 si no jugó.</param>
public record ResultadoDeFamiliaDto(int? GolesClub, int? GolesRival, bool Jugo, int Goles);
