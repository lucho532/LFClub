namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa el cálculo del porcentaje de asistencia de un jugador (spec 009, RF-030).
/// Su responsabilidad es dividir los entrenamientos a los que asistió entre aquellos en los que se
/// registró su asistencia y redondear al entero (la mitad, hacia arriba). Sin registros, el
/// porcentaje es cero, sin dividir por cero.
/// No cuenta nada: recibe los dos números ya calculados.
/// </summary>
public static class CalculoDeAsistencia
{
    /// <summary>Porcentaje de 0 a 100.</summary>
    public static int Porcentaje(int asistidos, int registrados) => registrados <= 0
        ? 0
        : (int)Math.Round(asistidos * 100m / registrados, MidpointRounding.AwayFromZero);
}
