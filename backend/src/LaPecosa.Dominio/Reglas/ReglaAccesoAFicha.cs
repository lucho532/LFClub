using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién ve y quién cambia la ficha de un jugador (constitución §7.5 y §8;
/// RF-005 a RF-010 y RF-016 a RF-019).
/// Su responsabilidad es resolver, en un único lugar, el alcance de quien pregunta: el PRESIDENTE
/// ve y cambia todo de cualquier jugador de su club; el DIRECTIVO ve cualquier ficha con sus
/// documentos, sin los datos clínicos y sin cambiar nada, también cuando entrena la categoría; el
/// ENTRENADOR ve, con los datos clínicos y sin los documentos, solo a los jugadores activos de una
/// categoría que entrena; y la cuenta del jugador ve y cambia la ficha del jugador de la petición,
/// menos los nombres, los apellidos y la fecha de nacimiento, y no la de un hermano (RF-030 de la
/// 006).
/// No consulta la base de datos ni conoce HTTP: recibe ya resueltos si el jugador es el de la
/// petición, si está activo y si quien pregunta entrena su categoría. No decide qué responde la
/// API a quien no tiene alcance.
/// </summary>
public static class ReglaAccesoAFicha
{
    /// <summary>
    /// Evalúa el alcance sobre la ficha de un jugador.
    /// <paramref name="esElJugadorDeLaPeticion"/> indica que el jugador de la ficha es el mismo
    /// integrante que pregunta;
    /// <paramref name="entrenaSuCategoria"/>, que quien pregunta tiene una asignación activa en la
    /// categoría activa del jugador.
    /// </summary>
    public static AlcanceDeFicha Evaluar(
        Rol rolDeQuienPregunta, bool esElJugadorDeLaPeticion, bool jugadorActivo, bool entrenaSuCategoria) =>
        rolDeQuienPregunta switch
        {
            Rol.PRESIDENTE => new AlcanceDeFicha(true, true, true, true, true),

            // La asignación como entrenador no le da los datos clínicos (RF-010).
            Rol.DIRECTIVO => new AlcanceDeFicha(true, false, true, false, false),

            Rol.ENTRENADOR when jugadorActivo && entrenaSuCategoria =>
                new AlcanceDeFicha(true, true, false, false, false),

            Rol.JUGADOR when esElJugadorDeLaPeticion => new AlcanceDeFicha(true, true, true, true, false),

            _ => AlcanceDeFicha.Ninguno,
        };
}
