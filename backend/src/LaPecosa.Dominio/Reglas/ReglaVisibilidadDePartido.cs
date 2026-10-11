using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién ve un partido (constitución §17.2; spec 009, RF-010, RF-014 y
/// RF-019; research §4).
/// Su responsabilidad es decidirlo con hechos ya resueltos: un partido jugado lo ve todo el club;
/// uno próximo o cancelado, solo el PRESIDENTE, quien entrena alguna de sus categorías (sea
/// ENTRENADOR o DIRECTIVO asignado) y la cuenta del jugador convocado. Para cualquier otro, el
/// partido no existe. El DESARROLLADOR no ve ninguno.
/// No consulta la base de datos. Los repositorios traducen esta misma condición a SQL para las
/// listas, el calendario y los contadores, y una prueba de integración compara las dos por rol.
/// </summary>
public static class ReglaVisibilidadDePartido
{
    /// <summary>Indica si quien pregunta ve el partido.</summary>
    /// <param name="rol">Rol de quien pregunta en el club.</param>
    /// <param name="estado">Estado actual del partido.</param>
    /// <param name="entrenaAlgunaDeSusCategorias">Si tiene una asignación activa en alguna categoría del partido.</param>
    /// <param name="estaConvocado">Si el jugador de la petición está convocado.</param>
    public static bool LoVe(Rol rol, EstadoPartido estado, bool entrenaAlgunaDeSusCategorias, bool estaConvocado)
    {
        if (rol == Rol.DESARROLLADOR)
        {
            return false;
        }

        if (estado == EstadoPartido.JUGADO)
        {
            return true;
        }

        return rol switch
        {
            Rol.PRESIDENTE => true,
            Rol.DIRECTIVO or Rol.ENTRENADOR => entrenaAlgunaDeSusCategorias,
            Rol.JUGADOR => estaConvocado,
            _ => false,
        };
    }
}
