namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa por qué un jugador aparece en la lista guardada de un entrenamiento, fuera de su
/// audiencia natural (spec 013; constitución §11.1 y §17.2).
/// Su responsabilidad es distinguir al jugador que suma una persona del que congela el sistema.
/// No decide quién ve el entrenamiento: eso es de la regla de audiencia.
/// </summary>
public enum OrigenDeJugadorEnEntrenamiento
{
    /// <summary>Lo sumó quien dirige el entrenamiento: ve y responde solo esa actividad.</summary>
    ANADIDO,

    /// <summary>
    /// Lo copió el sistema al desactivar el equipo al que iba dirigido el entrenamiento, para que no
    /// pierda lo que ya estaba convocado a ver. Solo lo escribe el sistema; ninguna persona lo
    /// escribe ni lo quita.
    /// </summary>
    EQUIPO_DESACTIVADO,
}
