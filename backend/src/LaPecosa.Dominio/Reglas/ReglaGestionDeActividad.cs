using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién gestiona un entrenamiento o un partido: programarlo o crearlo,
/// cambiarlo, cancelarlo, reactivarlo, convocar y registrar el resultado (spec 009, RF-002, RF-008
/// y RF-016; constitución 4.6.0, §8 "Entrenador").
/// Su responsabilidad es decidirlo con el rol y las categorías que entrena quien pregunta: el
/// PRESIDENTE siempre; un DIRECTIVO o un ENTRENADOR, solo si entrena todas las categorías de la
/// actividad (una asignación activa en cada una); el JUGADOR y el DESARROLLADOR, nunca.
/// No consulta la base de datos, no decide quién ve la actividad (ver
/// <see cref="ReglaVisibilidadDeEntrenamiento"/> y <see cref="ReglaVisibilidadDePartido"/>) y no
/// comprueba el estado de la actividad.
/// </summary>
public static class ReglaGestionDeActividad
{
    /// <summary>
    /// Indica si quien pregunta gestiona una actividad de esas categorías. Una actividad sin
    /// categorías no la gestiona nadie.
    /// </summary>
    public static bool PuedeGestionar(
        Rol rol, IReadOnlyCollection<Guid> categoriasDeLaActividad, IReadOnlySet<Guid> categoriasQueEntrena)
    {
        if (categoriasDeLaActividad.Count == 0)
        {
            return false;
        }

        return rol switch
        {
            Rol.PRESIDENTE => true,
            Rol.DIRECTIVO or Rol.ENTRENADOR => categoriasDeLaActividad.All(categoriasQueEntrena.Contains),
            _ => false,
        };
    }
}
