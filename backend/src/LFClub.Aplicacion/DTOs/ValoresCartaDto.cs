namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa las seis estadísticas de una carta que las tiene (spec 010, RF-022).
/// Su responsabilidad es llevarlas juntas, de 1 a 99.
/// No lleva la valoración, que va aparte en <see cref="CartaDto"/>.
/// </summary>
/// <param name="Ritmo">Ritmo.</param>
/// <param name="Tiro">Tiro.</param>
/// <param name="Pase">Pase.</param>
/// <param name="Regate">Regate.</param>
/// <param name="Defensa">Defensa.</param>
/// <param name="Fisico">Físico.</param>
public record ValoresCartaDto(int Ritmo, int Tiro, int Pase, int Regate, int Defensa, int Fisico);
