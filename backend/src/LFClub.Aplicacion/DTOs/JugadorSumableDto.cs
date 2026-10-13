namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador que se puede sumar a un entrenamiento o a un partido, o que ya está
/// añadido a un entrenamiento (spec 013, RF-013 y RF-021; research §9).
/// Su responsabilidad es llevar lo mínimo para elegirlo: su identificador, sus nombres y apellidos y
/// su categoría con el año.
/// No lleva foto, documento, equipos ni enlace a la ficha: sumarlo no da acceso a nada más de él
/// (constitución §7.5).
/// </summary>
/// <param name="UsuarioRolId">Identificador del jugador.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="Categoria">Su categoría actual, con el año.</param>
public record JugadorSumableDto(Guid UsuarioRolId, string Nombres, string Apellidos, CategoriaDeActividadDto Categoria);
