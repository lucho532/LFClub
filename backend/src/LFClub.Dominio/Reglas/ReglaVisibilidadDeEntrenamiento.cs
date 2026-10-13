using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién ve un entrenamiento (spec 009, RF-003; spec 013, RF-004; research
/// §4 de la 009 y §3 de la 013).
/// Su responsabilidad es decidirlo con hechos ya resueltos: el PRESIDENTE ve todos; el ENTRENADOR y
/// el DIRECTIVO, los que tienen alguna categoría que entrenan, y el DIRECTIVO sin asignar además los
/// que no están dirigidos a equipos (constitución §17.2); la cuenta de un jugador, los de su
/// audiencia (sin equipos, su categoría actual; con equipos, estar en alguno), aquellos en los que
/// está añadido o congelado y aquellos en los que ya tiene asistencia registrada; el DESARROLLADOR,
/// ninguno. Un entrenamiento cancelado lo siguen viendo los mismos.
/// No consulta la base de datos. Los repositorios traducen esta misma condición a SQL para las
/// listas, el calendario y los próximos, y una prueba de integración compara las dos por rol.
/// </summary>
public static class ReglaVisibilidadDeEntrenamiento
{
    /// <summary>Indica si quien pregunta ve el entrenamiento.</summary>
    /// <param name="rol">Rol de quien pregunta en el club.</param>
    /// <param name="entrenaAlgunaDeSusCategorias">Si tiene una asignación activa en alguna categoría del entrenamiento.</param>
    /// <param name="suCategoriaEstaEntreLasDelEntrenamiento">Si la categoría actual del jugador de la petición es una de las del entrenamiento.</param>
    /// <param name="tieneAsistenciaRegistrada">Si el jugador de la petición tiene asistencia registrada en él.</param>
    /// <param name="tieneEquipos">Si el entrenamiento está dirigido a equipos.</param>
    /// <param name="estaEnAlgunoDeSusEquipos">Si el jugador de la petición está en alguno de esos equipos.</param>
    /// <param name="estaEnLaListaGuardada">Si el jugador de la petición está añadido o congelado en el entrenamiento.</param>
    public static bool LaVe(
        Rol rol,
        bool entrenaAlgunaDeSusCategorias,
        bool suCategoriaEstaEntreLasDelEntrenamiento,
        bool tieneAsistenciaRegistrada,
        bool tieneEquipos = false,
        bool estaEnAlgunoDeSusEquipos = false,
        bool estaEnLaListaGuardada = false) => rol switch
        {
            Rol.PRESIDENTE => true,
            Rol.DIRECTIVO => entrenaAlgunaDeSusCategorias || !tieneEquipos,
            Rol.ENTRENADOR => entrenaAlgunaDeSusCategorias,
            Rol.JUGADOR => estaEnLaListaGuardada
                || tieneAsistenciaRegistrada
                || (tieneEquipos ? estaEnAlgunoDeSusEquipos : suCategoriaEstaEntreLasDelEntrenamiento),
            _ => false,
        };
}
