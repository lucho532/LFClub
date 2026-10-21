using System.Globalization;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa el texto en español de un importe en pesos colombianos y de los meses (spec 015).
/// Su responsabilidad es escribir "65.000" con puntos de miles y sin decimales, y nombrar los meses, sin
/// depender de la cultura ni de los datos de ICU del servidor, que un contenedor mínimo puede no traer.
/// No agrega el signo de pesos ni decide qué se escribe.
/// </summary>
public static class TextoDePesos
{
    /// <summary>Nombres de los meses, en minúsculas.</summary>
    public static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
    ];

    /// <summary>El valor redondeado a pesos enteros, con puntos de miles: 65000 → "65.000".</summary>
    public static string ConPuntos(decimal valor) =>
        decimal.Round(valor, 0, MidpointRounding.AwayFromZero)
            .ToString("N0", CultureInfo.InvariantCulture)
            .Replace(",", ".", StringComparison.Ordinal);
}
