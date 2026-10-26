namespace LFClub.Dominio.Reglas;

/// <summary>Conteo de integrantes activos por rol de un club.</summary>
public record ConteoPorRol(int Jugadores, int Entrenadores, int Directivos, int Presidentes);

/// <summary>
/// Un rango de la tarifa: de <paramref name="Desde"/> a <paramref name="Hasta"/> jugadores (sin límite
/// si <paramref name="Hasta"/> es nulo) el club paga <paramref name="Precio"/> pesos al mes.
/// </summary>
public record RangoTarifa(int Desde, int? Hasta, decimal Precio);

/// <summary>El cobro calculado: el conteo del club, el rango que le tocó y el total.</summary>
public record CalculoDeCobro(ConteoPorRol Conteo, RangoTarifa Rango, decimal Total);

/// <summary>
/// Representa la regla del valor del cobro de la plataforma (spec 017, RF-003).
/// Su responsabilidad es buscar el rango de la tarifa que corresponde a la cantidad de jugadores
/// activos del club y tomar su precio fijo, y decidir si hay cobro: no lo hay si ningún rango cubre
/// esa cantidad o si el precio del rango es 0. Entrenadores, directivos y presidentes no suman.
/// No lee la base de datos, no guarda nada y no conoce fechas.
/// </summary>
public static class ReglaCobroPlataforma
{
    /// <summary>El rango que cubre esa cantidad de jugadores, o nulo si ninguno.</summary>
    public static RangoTarifa? BuscarRango(int jugadores, IEnumerable<RangoTarifa> rangos) =>
        rangos.FirstOrDefault(rango => jugadores >= rango.Desde && (rango.Hasta is null || jugadores <= rango.Hasta));

    /// <summary>Calcula el cobro, o devuelve nulo si no hay nada que cobrar.</summary>
    public static CalculoDeCobro? Calcular(ConteoPorRol conteo, IEnumerable<RangoTarifa> rangos) =>
        BuscarRango(conteo.Jugadores, rangos) is { Precio: > 0 } rango
            ? new CalculoDeCobro(conteo, rango, rango.Precio)
            : null;
}
