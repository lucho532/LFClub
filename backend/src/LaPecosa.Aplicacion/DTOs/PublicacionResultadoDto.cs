namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa quién puso el marcador de un partido por primera vez y cuándo (spec 010, RF-009).
/// Su responsabilidad es darle a quien ve la planilla el nombre copiado de quien lo publicó (el del
/// jugador si fue su familia) y si fue una familia.
/// No llega nunca a la cuenta de un jugador ni a quien no ve la planilla (escenario 1.11), y no
/// cambia cuando después se corrige el resultado.
/// </summary>
/// <param name="Fecha">Fecha y hora de la primera publicación, en UTC.</param>
/// <param name="Autor">Nombre de quien lo publicó, tal como era en ese momento.</param>
/// <param name="PorFamilia">Verdadero si lo publicó la familia de un convocado.</param>
public record PublicacionResultadoDto(DateTime Fecha, string Autor, bool PorFamilia);
