namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se envía al poner, cambiar o quitar las seis estadísticas de la carta (spec
/// 010, RF-022).
/// Su responsabilidad es llevar las seis: enteras de 1 a 99, o las seis nulas para dejar la carta
/// sin números.
/// No valida nada: lo hace <c>ReglaEstadisticasDeCarta</c>. Un decimal no se puede leer y responde
/// 400 <c>datos_invalidos</c> antes de llegar al servicio.
/// </summary>
/// <param name="Ritmo">Ritmo.</param>
/// <param name="Tiro">Tiro.</param>
/// <param name="Pase">Pase.</param>
/// <param name="Regate">Regate.</param>
/// <param name="Defensa">Defensa.</param>
/// <param name="Fisico">Físico.</param>
public record EstadisticasCartaDto(int? Ritmo, int? Tiro, int? Pase, int? Regate, int? Defensa, int? Fisico);
