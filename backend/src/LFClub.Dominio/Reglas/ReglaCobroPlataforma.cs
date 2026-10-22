namespace LFClub.Dominio.Reglas;

/// <summary>Conteo de integrantes activos por rol de un club.</summary>
public record ConteoPorRol(int Jugadores, int Entrenadores, int Directivos, int Presidentes);

/// <summary>Los cuatro precios por rol; un precio nulo cuenta como 0.</summary>
public record PreciosPorRol(
    decimal? Jugador, decimal? Entrenador, decimal? Directivo, decimal? Presidente);

/// <summary>El cobro calculado: los precios aplicados (nulos como 0) y el total.</summary>
public record CalculoDeCobro(ConteoPorRol Conteo, decimal PrecioJugador, decimal PrecioEntrenador,
    decimal PrecioDirectivo, decimal PrecioPresidente, decimal Total);

/// <summary>
/// Representa la regla del valor del cobro de la plataforma (spec 017, RF-003).
/// Su responsabilidad es sumar, por rol, los integrantes activos por su precio, y decidir si hay
/// cobro: no lo hay si todos los precios están sin definir o si el total es 0.
/// No lee la base de datos, no guarda nada y no conoce fechas.
/// </summary>
public static class ReglaCobroPlataforma
{
    /// <summary>Calcula el cobro, o devuelve nulo si no hay nada que cobrar.</summary>
    public static CalculoDeCobro? Calcular(ConteoPorRol conteo, PreciosPorRol precios)
    {
        if (precios.Jugador is null && precios.Entrenador is null
            && precios.Directivo is null && precios.Presidente is null)
        {
            return null;
        }

        var jugador = precios.Jugador ?? 0;
        var entrenador = precios.Entrenador ?? 0;
        var directivo = precios.Directivo ?? 0;
        var presidente = precios.Presidente ?? 0;

        var total = (conteo.Jugadores * jugador) + (conteo.Entrenadores * entrenador)
            + (conteo.Directivos * directivo) + (conteo.Presidentes * presidente);

        return total > 0 ? new CalculoDeCobro(conteo, jugador, entrenador, directivo, presidente, total) : null;
    }
}
