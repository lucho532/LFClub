using LFClub.Dominio.Enumeraciones;
using SkiaSharp;

namespace LFClub.Infraestructura.Imagenes;

/// <summary>
/// Representa las capas de la carta que no son texto (contracts/carta.md): el reflejo, los destellos
/// de luz, el marco metálico y las imágenes sin recuadro (la foto fundida con el fondo y el
/// escudo en un óvalo de borde suave), dibujadas con SkiaSharp con los números de
/// <see cref="PlanoCarta"/>, los mismos que la pantalla.
/// Su responsabilidad es pintar cada capa en su sitio; el orden lo pone <see cref="GeneradorImagenCarta"/>.
/// No escribe texto ni decide colores del texto.
/// </summary>
internal static class CapasCarta
{
    private static readonly SKSamplingOptions Muestreo = new(SKFilterMode.Linear, SKMipmapMode.Linear);
    private static readonly SKColor Opaco = SKColors.Black;
    private static readonly SKColor Transparente = SKColors.Black.WithAlpha(0);

    /// <summary>Reflejo blanco: bandas diagonales como <c>linear-gradient</c> de CSS y halo arriba a la izquierda.</summary>
    public static void Brillo(SKCanvas lienzo)
    {
        var sinBlanco = SKColors.White.WithAlpha(0);
        var angulo = PlanoCarta.AnguloBrillo * MathF.PI / 180;
        var (dx, dy) = (MathF.Sin(angulo), -MathF.Cos(angulo));
        var largo = (MathF.Abs(PlanoCarta.Ancho * dx) + MathF.Abs(PlanoCarta.Alto * dy)) / 2;
        var centro = new SKPoint(PlanoCarta.Ancho / 2f, PlanoCarta.Alto / 2f);
        foreach (var (inicio, maximo, fin, opacidad) in PlanoCarta.BandasBrillo)
        {
            using var banda = new SKPaint { IsAntialias = true };
            banda.Shader = SKShader.CreateLinearGradient(
                new SKPoint(centro.X - (dx * largo), centro.Y - (dy * largo)),
                new SKPoint(centro.X + (dx * largo), centro.Y + (dy * largo)),
                [sinBlanco, sinBlanco, SKColors.White.WithAlpha(Alfa(opacidad)), sinBlanco, sinBlanco],
                [0, inicio, maximo, fin, 1],
                SKShaderTileMode.Clamp);
            lienzo.DrawRect(SKRect.Create(PlanoCarta.Ancho, PlanoCarta.Alto), banda);
        }

        using var halo = new SKPaint { IsAntialias = true };
        halo.Shader = SKShader.CreateRadialGradient(
            new SKPoint(PlanoCarta.HaloBrilloX, PlanoCarta.HaloBrilloY),
            PlanoCarta.HaloBrilloRadio,
            [SKColors.White.WithAlpha(Alfa(PlanoCarta.OpacidadHaloBrillo)), sinBlanco],
            SKShaderTileMode.Clamp);
        lienzo.DrawRect(SKRect.Create(PlanoCarta.Ancho, PlanoCarta.Alto), halo);
    }

    /// <summary>Destellos de luz: estrellas blancas de cuatro puntas con un resplandor suave (los de Destellos.tsx).</summary>
    public static void Destellos(SKCanvas lienzo)
    {
        using var estrella = new SKPaint { Color = SKColors.White, IsAntialias = true };
        foreach (var (x, y, tamano) in PlanoCarta.Destellos)
        {
            using var resplandor = new SKPaint { IsAntialias = true };
            resplandor.Shader = SKShader.CreateRadialGradient(
                new SKPoint(x, y),
                tamano * PlanoCarta.ResplandorDestello,
                [SKColors.White.WithAlpha(Alfa(PlanoCarta.OpacidadResplandorDestello)), SKColors.White.WithAlpha(0)],
                SKShaderTileMode.Clamp);
            lienzo.DrawCircle(x, y, tamano * PlanoCarta.ResplandorDestello, resplandor);

            var c = tamano * PlanoCarta.CinturaDestello;
            using var trazado = new SKPath();
            trazado.MoveTo(x, y - tamano);
            trazado.LineTo(x + c, y - c);
            trazado.LineTo(x + tamano, y);
            trazado.LineTo(x + c, y + c);
            trazado.LineTo(x, y + tamano);
            trazado.LineTo(x - c, y + c);
            trazado.LineTo(x - tamano, y);
            trazado.LineTo(x - c, y - c);
            trazado.Close();
            lienzo.DrawPath(trazado, estrella);
        }
    }

    /// <summary>Marco metálico de 18 hacia dentro, con el degradado de la rareza de arriba abajo.</summary>
    public static void Marco(SKCanvas lienzo, RarezaCarta rareza)
    {
        var (claro, medio, oscuro) = PlanoCarta.TonosDelMarco(rareza);
        var mitad = PlanoCarta.Marco / 2;
        using var pintura = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = PlanoCarta.Marco, IsAntialias = true };
        pintura.Shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(0, PlanoCarta.Alto),
            [SKColor.Parse(claro), SKColor.Parse(medio), SKColor.Parse(oscuro)],
            SKShaderTileMode.Clamp);
        var rectangulo = new SKRect(mitad, mitad, PlanoCarta.Ancho - mitad, PlanoCarta.Alto - mitad);
        lienzo.DrawRoundRect(new SKRoundRect(rectangulo, PlanoCarta.Radio - mitad), pintura);
    }

    /// <summary>
    /// La foto recortada al centro para llenar la caja y fundida con el fondo (abajo y a los lados).
    /// Devuelve falso si no hay foto o no se puede leer.
    /// </summary>
    public static bool Foto(SKCanvas lienzo, byte[]? bytes, Caja caja) => ConMascara(lienzo, bytes, caja, cubrir: true, area =>
    {
        Enmascarar(lienzo, area, SKShader.CreateLinearGradient(
            new SKPoint(area.Left, area.Top), new SKPoint(area.Left, area.Bottom),
            [Opaco, Opaco, Transparente], [0, PlanoCarta.FotoOpacaHasta, 1], SKShaderTileMode.Clamp));
        Enmascarar(lienzo, area, SKShader.CreateLinearGradient(
            new SKPoint(area.Left, area.Top), new SKPoint(area.Right, area.Top),
            [Transparente, Opaco, Opaco, Transparente], [0, PlanoCarta.FotoLados, 1 - PlanoCarta.FotoLados, 1], SKShaderTileMode.Clamp));
    });

    /// <summary>El escudo entero, sin deformar, en un óvalo de borde suave: no se ve el recuadro de la imagen.</summary>
    public static void Escudo(SKCanvas lienzo, byte[]? bytes, Caja caja) => ConMascara(lienzo, bytes, caja, cubrir: false, area =>
        Enmascarar(lienzo, area, SKShader.CreateRadialGradient(
            new SKPoint(area.MidX, area.MidY), Math.Min(area.Width, area.Height) / 2,
            [Opaco, Opaco, Transparente], [0, PlanoCarta.EscudoOpaco, 1], SKShaderTileMode.Clamp)));

    private static bool ConMascara(SKCanvas lienzo, byte[]? bytes, Caja caja, bool cubrir, Action<SKRect> mascara)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return false;
        }

        using var imagen = SKImage.FromEncodedData(bytes);
        if (imagen is null)
        {
            return false;
        }

        var area = Rectangulo(caja);
        var escala = cubrir
            ? Math.Max(caja.Ancho / imagen.Width, caja.Alto / imagen.Height)
            : Math.Min(caja.Ancho / imagen.Width, caja.Alto / imagen.Height);
        var (ancho, alto) = (imagen.Width * escala, imagen.Height * escala);
        lienzo.SaveLayer(area, null);
        lienzo.ClipRect(area);
        lienzo.DrawImage(imagen, SKRect.Create(area.MidX - (ancho / 2), area.MidY - (alto / 2), ancho, alto), Muestreo);
        mascara(area);
        lienzo.Restore();
        return true;
    }

    // Deja de la capa solo lo que el sombreador deja opaco.
    private static void Enmascarar(SKCanvas lienzo, SKRect area, SKShader sombreador)
    {
        using var conSombreador = sombreador;
        using var pintura = new SKPaint { Shader = sombreador, BlendMode = SKBlendMode.DstIn };
        lienzo.DrawRect(area, pintura);
    }

    private static SKRect Rectangulo(Caja caja) => SKRect.Create(caja.X, caja.Y, caja.Ancho, caja.Alto);

    private static byte Alfa(float opacidad) => (byte)Math.Round(255 * opacidad);
}
