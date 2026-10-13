using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa lo que hace falta saber de quien hace la petición para decidir qué entrenamientos y
/// partidos ve (spec 009, research §3 y §4).
/// Su responsabilidad es llevar el integrante de la petición (el jugador elegido, nunca la cuenta),
/// su rol, su categoría actual y sus equipos actuales si es la cuenta de un jugador y las categorías
/// que entrena hoy, ya leídas de sus asignaciones activas.
/// No decide nada: lo usan las reglas de visibilidad y los repositorios que las traducen a SQL.
/// </summary>
/// <param name="UsuarioRolId">Integrante de la petición.</param>
/// <param name="Rol">Su rol en el club.</param>
/// <param name="CategoriaId">Su categoría actual, solo si es la cuenta de un jugador.</param>
/// <param name="CategoriasQueEntrena">Categorías en las que tiene una asignación activa.</param>
/// <param name="EquipoIds">Equipos activos en los que juega, solo si es la cuenta de un jugador (spec 013).</param>
public record QuienPregunta(
    Guid UsuarioRolId, Rol Rol, Guid? CategoriaId, IReadOnlySet<Guid> CategoriasQueEntrena, IReadOnlySet<Guid> EquipoIds)
{
    /// <summary>Indica si es la cuenta de un jugador.</summary>
    public bool EsJugador => Rol == Rol.JUGADOR;
}
