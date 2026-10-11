using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa la lectura de lo que forma la ficha básica de una persona del club de la petición
/// (spec 008).
/// Su responsabilidad es encontrar al integrante PRESIDENTE, DIRECTIVO o ENTRENADOR aprobado, con la
/// versión de su foto, y leer sus asignaciones activas de categorías activas con los equipos activos
/// que dirige. Trabaja siempre con el filtro de aislamiento: nunca ve otro club.
/// No modifica datos, no decide quién puede ver la ficha y no lee a los integrantes JUGADOR.
/// </summary>
public interface IRepositorioFichaBasica
{
    /// <summary>
    /// El integrante con la versión de su foto; nulo si no existe en este club, si es JUGADOR o si
    /// su ingreso no está aprobado.
    /// </summary>
    Task<PersonaConFoto?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Sus asignaciones activas de categorías activas, con la categoría y solo los equipos activos
    /// que dirige, cada uno con su equipo cargado.
    /// </summary>
    Task<IReadOnlyList<AsignacionEntrenadorCategoria>> AsignacionesActivasAsync(
        Guid usuarioRolId, CancellationToken cancelacion = default);
}
