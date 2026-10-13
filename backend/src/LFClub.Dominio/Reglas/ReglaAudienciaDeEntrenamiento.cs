namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de qué jugadores forman la audiencia de un entrenamiento (spec 013, RF-002,
/// RF-003 y RF-013; research §3 y §7; constitución §11.1 y §17.2).
/// Su responsabilidad es decidir con hechos ya resueltos si un jugador pertenece a la audiencia:
/// sin equipos, por su categoría actual; con equipos, por estar en alguno de ellos (la categoría
/// sola no basta); y siempre si está en la lista guardada (añadido o congelado por un equipo
/// desactivado). Un jugador inactivo, retirado o sin categoría no entra por ningún camino.
/// No consulta la base de datos, no decide quién ve el entrenamiento (ver
/// <see cref="ReglaVisibilidadDeEntrenamiento"/>) y la asistencia ya registrada no lo vuelve parte
/// de la audiencia: solo le permite verlo.
/// </summary>
public static class ReglaAudienciaDeEntrenamiento
{
    /// <summary>Indica si el jugador pertenece a la audiencia, una sola vez aunque entre por varios caminos.</summary>
    /// <param name="activo">Si el jugador es una cuenta activa, aprobada y no retirada.</param>
    /// <param name="tieneCategoria">Si el jugador tiene una categoría actual.</param>
    /// <param name="entrenamientoTieneEquipos">Si el entrenamiento tiene equipos elegidos.</param>
    /// <param name="estaEnAlgunDeSusEquipos">Si el jugador está en alguno de los equipos del entrenamiento.</param>
    /// <param name="suCategoriaEsDelEntrenamiento">Si su categoría actual es una de las del entrenamiento.</param>
    /// <param name="estaEnLaListaGuardada">Si tiene fila en la lista de jugadores del entrenamiento, añadido o congelado.</param>
    public static bool EnLaAudiencia(
        bool activo,
        bool tieneCategoria,
        bool entrenamientoTieneEquipos,
        bool estaEnAlgunDeSusEquipos,
        bool suCategoriaEsDelEntrenamiento,
        bool estaEnLaListaGuardada)
    {
        if (!activo || !tieneCategoria)
        {
            return false;
        }

        if (estaEnLaListaGuardada)
        {
            return true;
        }

        return entrenamientoTieneEquipos ? estaEnAlgunDeSusEquipos : suCategoriaEsDelEntrenamiento;
    }
}
