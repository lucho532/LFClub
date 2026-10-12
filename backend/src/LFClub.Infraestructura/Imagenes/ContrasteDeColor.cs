using System.Globalization;
using System.Text.RegularExpressions;

namespace LFClub.Infraestructura.Imagenes;

/// <summary>
/// Representa el cálculo de colores de la carta (contracts/carta.md), el mismo que hace la pantalla
/// con <c>contraste.ts</c> y <c>plano.ts</c>.
/// Su responsabilidad es mezclar colores, calcular la luminancia y el contraste según WCAG, armar los
/// dos extremos del degradado y elegir el color del texto y, si hace falta, el de su sombra.
/// No dibuja nada ni conoce SkiaSharp: trabaja con colores <c>#RRGGBB</c>.
/// </summary>
public static partial class ContrasteDeColor
{
    /// <summary>Texto claro de la carta.</summary>
    public const string TextoClaro = "#FFFFFF";

    /// <summary>Texto oscuro de la carta (casi negro).</summary>
    public const string TextoOscuro = "#141414";

    /// <summary>Por debajo de este contraste con un extremo del degradado, el texto lleva sombra.</summary>
    public const double ContrasteSinSombra = 4.5;

    // Neutros del tema claro (variables.css), para un club sin colores.
    private const string NeutroPrincipal = "#231F1E";
    private const string NeutroAcento = "#5E5650";

    /// <summary>Indica si el texto es un color #RRGGBB.</summary>
    public static bool EsColor(string? color) => color is not null && Formato().IsMatch(color);

    /// <summary>Mezcla dos colores: con <paramref name="peso"/> 1 queda el primero y con 0 el segundo.</summary>
    public static string Mezclar(string uno, string otro, double peso)
    {
        var canales = Enumerable.Range(0, 3).Select(indice =>
            (int)Math.Round((Canal(uno, indice) * peso) + (Canal(otro, indice) * (1 - peso)), MidpointRounding.AwayFromZero));
        return "#" + string.Concat(canales.Select(valor => valor.ToString("X2", CultureInfo.InvariantCulture)));
    }

    /// <summary>Luminancia relativa, de 0 (negro) a 1 (blanco).</summary>
    public static double Luminancia(string color)
    {
        static double Lineal(int valor)
        {
            var proporcion = valor / 255d;
            return proporcion <= 0.03928 ? proporcion / 12.92 : Math.Pow((proporcion + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Lineal(Canal(color, 0))) + (0.7152 * Lineal(Canal(color, 1))) + (0.0722 * Lineal(Canal(color, 2)));
    }

    /// <summary>Contraste entre dos colores, de 1 a 21.</summary>
    public static double Contraste(string uno, string otro)
    {
        var a = Luminancia(uno);
        var b = Luminancia(otro);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>
    /// Los dos extremos del degradado: principal y acento; sin acento, el principal y su versión 35 %
    /// más oscura; sin colores, los neutros.
    /// </summary>
    public static (string Desde, string Hasta) Degradado(string? principal, string? acento)
    {
        if (!EsColor(principal))
        {
            return (NeutroPrincipal, NeutroAcento);
        }

        var inicio = principal!.ToUpperInvariant();
        return (inicio, EsColor(acento) ? acento!.ToUpperInvariant() : Mezclar(inicio, "#000000", 0.65));
    }

    /// <summary>
    /// El color del texto (el de más contraste con el color medio del degradado) y el de su sombra,
    /// nulo si el contraste con los dos extremos llega a 4,5:1.
    /// </summary>
    public static (string Texto, string? Sombra) ColorDelTexto((string Desde, string Hasta) degradado)
    {
        var medio = Mezclar(degradado.Desde, degradado.Hasta, 0.5);
        var claro = Contraste(medio, TextoClaro) >= Contraste(medio, TextoOscuro);
        var texto = claro ? TextoClaro : TextoOscuro;
        var peor = Math.Min(Contraste(texto, degradado.Desde), Contraste(texto, degradado.Hasta));
        return (texto, peor < ContrasteSinSombra ? (claro ? TextoOscuro : TextoClaro) : null);
    }

    private static int Canal(string color, int indice) =>
        int.Parse(color.AsSpan(1 + (indice * 2), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex Formato();
}
