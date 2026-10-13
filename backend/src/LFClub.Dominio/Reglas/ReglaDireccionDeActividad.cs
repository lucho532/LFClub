using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién dirige un entrenamiento: quien puede cambiar a qué equipos va y a
/// qué jugadores se suman (spec 013, RF-016 y RF-020; research §4; constitución §8 y §17.2).
/// Su responsabilidad es decidirlo con el rol y las categorías que entrena quien pregunta: el
/// PRESIDENTE siempre; un DIRECTIVO o un ENTRENADOR si entrena <b>alguna</b> de las categorías de
/// la actividad; el JUGADOR y el DESARROLLADOR, nunca.
/// Se diferencia de <see cref="ReglaGestionDeActividad"/>, que exige entrenar <b>todas</b> las
/// categorías: programar, cambiar datos, cancelar y convocar siguen siendo de quien gestiona.
/// No consulta la base de datos ni comprueba el estado de la actividad.
/// </summary>
public static class ReglaDireccionDeActividad
{
    /// <summary>Indica si quien pregunta dirige una actividad de esas categorías.</summary>
    public static bool PuedeDirigir(
        Rol rol, IReadOnlyCollection<Guid> categoriasDeLaActividad, IReadOnlySet<Guid> categoriasQueEntrena) => rol switch
        {
            Rol.PRESIDENTE => true,
            Rol.DIRECTIVO or Rol.ENTRENADOR => categoriasDeLaActividad.Any(categoriasQueEntrena.Contains),
            _ => false,
        };
}
