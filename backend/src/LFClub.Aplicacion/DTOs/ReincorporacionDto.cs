namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el resultado de reincorporar a un jugador retirado (RF-045).
/// Su responsabilidad es decir en qué categoría quedó, o que quedó sin categoría.
/// No devuelve equipos: al reincorporarse no recupera los que tenía.
/// </summary>
/// <param name="UsuarioRolId">Identificador del integrante en este club.</param>
/// <param name="CategoriaId">Categoría en la que quedó; nulo si quedó sin categoría.</param>
/// <param name="Anio">Año de esa categoría; nulo si quedó sin categoría.</param>
public record ReincorporacionDto(Guid UsuarioRolId, Guid? CategoriaId, int? Anio);
