using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién ve un entrenamiento (spec 009, RF-003; research §4).
/// Su responsabilidad es decidirlo con hechos ya resueltos: el PRESIDENTE y el DIRECTIVO ven todos;
/// el ENTRENADOR, los que tienen alguna categoría que entrena; la cuenta de un jugador, los de la
/// categoría actual del jugador de la petición y aquellos en los que ya tiene asistencia registrada;
/// el DESARROLLADOR, ninguno. Un entrenamiento cancelado lo siguen viendo los mismos.
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
    public static bool LaVe(
        Rol rol,
        bool entrenaAlgunaDeSusCategorias,
        bool suCategoriaEstaEntreLasDelEntrenamiento,
        bool tieneAsistenciaRegistrada) => rol switch
        {
            Rol.PRESIDENTE or Rol.DIRECTIVO => true,
            Rol.ENTRENADOR => entrenaAlgunaDeSusCategorias,
            Rol.JUGADOR => suCategoriaEstaEntreLasDelEntrenamiento || tieneAsistenciaRegistrada,
            _ => false,
        };
}
