using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa el cálculo de la rareza de la carta de un jugador por su asistencia a los
/// entrenamientos de la temporada (spec 010, RF-026; research §10).
/// Su responsabilidad es convertir el porcentaje ya redondeado con <see cref="CalculoDeAsistencia"/>
/// en un nivel: menos de 60, Bronce; de 60 a 84, Plata; 85 o más, Oro. Sin asistencias registradas,
/// Bronce.
/// No cuenta asistencias ni decide qué es la temporada: recibe los números ya calculados.
/// </summary>
public static class ReglaRarezaDeCarta
{
    /// <summary>La rareza para esos registros y ese porcentaje (nulo o cualquiera si no hay registros).</summary>
    public static RarezaCarta Calcular(int registrados, int? porcentaje) => registrados <= 0 || porcentaje is null
        ? RarezaCarta.BRONCE
        : porcentaje.Value switch
        {
            >= 85 => RarezaCarta.ORO,
            >= 60 => RarezaCarta.PLATA,
            _ => RarezaCarta.BRONCE,
        };
}
