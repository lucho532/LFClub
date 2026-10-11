using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de qué categorías de un entrenamiento puede ver en la planilla y marcar en
/// la asistencia quien pregunta (spec 009, RF-022, escenario 4.2).
/// Su responsabilidad es decidirlo con el rol y las categorías que entrena: el PRESIDENTE, todas
/// las del entrenamiento; un DIRECTIVO o un ENTRENADOR, solo las del entrenamiento que entrena; el
/// JUGADOR y el DESARROLLADOR, ninguna.
/// No consulta la base de datos ni comprueba el estado o la fecha del entrenamiento.
/// </summary>
public static class ReglaAlcanceDeAsistencia
{
    /// <summary>Las categorías del entrenamiento que quien pregunta puede marcar.</summary>
    public static IReadOnlySet<Guid> CategoriasMarcables(
        Rol rol, IReadOnlyCollection<Guid> categoriasDelEntrenamiento, IReadOnlySet<Guid> categoriasQueEntrena) =>
        rol switch
        {
            Rol.PRESIDENTE => categoriasDelEntrenamiento.ToHashSet(),
            Rol.DIRECTIVO or Rol.ENTRENADOR => categoriasDelEntrenamiento.Where(categoriasQueEntrena.Contains).ToHashSet(),
            _ => new HashSet<Guid>(),
        };
}
