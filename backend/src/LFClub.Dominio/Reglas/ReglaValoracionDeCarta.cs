namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa el cálculo de la valoración de la carta de un jugador (spec 010, RF-023).
/// Su responsabilidad es promediar las seis estadísticas y redondear al entero, la mitad hacia
/// arriba, como <see cref="CalculoDeAsistencia"/> (72,5 da 73). Sin estadísticas, no hay valoración.
/// No se guarda ni valida los valores: los recibe ya validados.
/// </summary>
public static class ReglaValoracionDeCarta
{
    /// <summary>La valoración, o nula si la carta no tiene estadísticas.</summary>
    public static int? Calcular(int? ritmo, int? tiro, int? pase, int? regate, int? defensa, int? fisico)
    {
        int?[] valores = [ritmo, tiro, pase, regate, defensa, fisico];
        if (valores.Any(valor => valor is null))
        {
            return null;
        }

        var suma = valores.Sum(valor => valor!.Value);
        return (int)Math.Round(suma / 6m, MidpointRounding.AwayFromZero);
    }
}
