using LFClub.Dominio.Entidades;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de unas estadísticas de carta válidas (spec 010, RF-022).
/// Su responsabilidad es aceptar las seis enteras de 1 a 99, o las seis nulas (la carta queda sin
/// números), y rechazar cualquier mezcla o valor fuera de rango diciendo qué campo falla.
/// No consulta la base de datos ni guarda nada; los decimales los rechaza antes la lectura del
/// cuerpo de la petición.
/// </summary>
public static class ReglaEstadisticasDeCarta
{
    /// <summary>Los nombres de los seis campos, en el orden del contrato.</summary>
    public static readonly IReadOnlyList<string> Campos = ["ritmo", "tiro", "pase", "regate", "defensa", "fisico"];

    /// <summary>
    /// Nulo si son válidas; si no, el nombre del primer campo que falla (el primero que falta, si
    /// vienen mezcladas).
    /// </summary>
    public static string? Validar(int? ritmo, int? tiro, int? pase, int? regate, int? defensa, int? fisico)
    {
        int?[] valores = [ritmo, tiro, pase, regate, defensa, fisico];
        if (valores.All(valor => valor is null))
        {
            return null;
        }

        for (var indice = 0; indice < valores.Length; indice++)
        {
            if (valores[indice] is not { } valor || valor < CartaJugador.ValorMinimo || valor > CartaJugador.ValorMaximo)
            {
                return Campos[indice];
            }
        }

        return null;
    }
}
