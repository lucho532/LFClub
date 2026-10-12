using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa quién indicó lo de un convocado (si jugó y sus goles) y cuándo (spec 010, RF-009).
/// Su responsabilidad es que quien ve la planilla sepa si fue su familia o quien entrena.
/// No existe si nadie indicó nada (RF-011), y no llega nunca a la cuenta de un jugador.
/// </summary>
/// <param name="Origen">La familia o quien gestiona.</param>
/// <param name="Autor">Nombre copiado de quien lo indicó; el del jugador si fue su familia.</param>
/// <param name="Fecha">Fecha y hora, en UTC.</param>
public record IndicacionDto(OrigenResultado Origen, string Autor, DateTime Fecha);
