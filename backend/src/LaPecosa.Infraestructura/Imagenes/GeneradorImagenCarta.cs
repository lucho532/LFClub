using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using SkiaSharp;

namespace LaPecosa.Infraestructura.Imagenes;

/// <summary>
/// Representa el dibujo de la carta como PNG de 750 × 1050 con SkiaSharp (spec 010, historia 4;
/// research §11), siguiendo <see cref="PlanoCarta"/>, el mismo plano que la pantalla.
/// Su responsabilidad es pintar, de atrás hacia delante, el degradado con las esquinas redondeadas
/// (fuera, transparente), el reflejo, el escudo en óvalo, la foto fundida o la
/// figura genérica (estas capas, con <see cref="CapasCarta"/>), la valoración, la posición, el dorsal, el nombre, la
/// línea, las estadísticas (con "–" sin números) y la rareza escrita, con Barlow Condensed leída de
/// los recursos incrustados.
/// No decide quién exporta ni añade ningún dato que no reciba: ni documento, ni fecha de nacimiento,
/// ni contacto, ni salud, ni categoría, ni porcentaje, ni fecha de exportación (RF-030).
/// </summary>
public sealed class GeneradorImagenCarta : IGeneradorImagenCarta
{
    private static readonly SKTypeface Negrita = Cargar("BarlowCondensed-Bold.ttf");
    private static readonly SKTypeface ExtraNegrita = Cargar("BarlowCondensed-ExtraBold.ttf");

    /// <inheritdoc />
    public byte[] Generar(DatosImagenCarta datos)
    {
        var info = new SKImageInfo(PlanoCarta.Ancho, PlanoCarta.Alto, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var superficie = SKSurface.Create(info);
        var lienzo = superficie.Canvas;
        lienzo.Clear(SKColors.Transparent);

        var degradado = ContrasteDeColor.Degradado(datos.ColorPrincipal, datos.ColorAcento);
        var (texto, sombra) = ContrasteDeColor.ColorDelTexto(degradado);
        var colorTexto = SKColor.Parse(texto);
        var borde = new SKRoundRect(new SKRect(0, 0, PlanoCarta.Ancho, PlanoCarta.Alto), PlanoCarta.Radio);

        lienzo.Save();
        lienzo.ClipRoundRect(borde, SKClipOperation.Intersect, true);
        Fondo(lienzo, degradado);
        CapasCarta.Brillo(lienzo);
        CapasCarta.Escudo(lienzo, datos.Escudo, PlanoCarta.Escudo);
        if (!CapasCarta.Foto(lienzo, datos.Foto, PlanoCarta.Foto))
        {
            Figura(lienzo, colorTexto);
        }

        using var pintura = new SKPaint { Color = colorTexto, IsAntialias = true };
        if (sombra is not null)
        {
            // Halo como el text-shadow de la pantalla: un contorno apretado sobre un resplandor.
            var halo = SKColor.Parse(sombra).WithAlpha((byte)(255 * PlanoCarta.OpacidadHalo));
            var resplandor = SKImageFilter.CreateDropShadow(0, 0, PlanoCarta.DesenfoqueHalo / 2, PlanoCarta.DesenfoqueHalo / 2, halo);
            pintura.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, PlanoCarta.ContornoHalo / 2, PlanoCarta.ContornoHalo / 2, halo, resplandor);
        }

        Textos(lienzo, datos, pintura);
        using var linea = new SKPaint { Color = colorTexto.WithAlpha((byte)(255 * PlanoCarta.OpacidadLinea)), IsAntialias = true };
        var cajaLinea = PlanoCarta.Linea;
        lienzo.DrawRect(new SKRect(cajaLinea.X, cajaLinea.Y, cajaLinea.X + cajaLinea.Ancho, cajaLinea.Y + cajaLinea.Alto), linea);
        CapasCarta.Marco(lienzo, datos.Rareza);
        CapasCarta.Destellos(lienzo);
        lienzo.Restore();

        using var imagen = superficie.Snapshot();
        using var png = imagen.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    private static void Fondo(SKCanvas lienzo, (string Desde, string Hasta) degradado)
    {
        using var pintura = new SKPaint { IsAntialias = true };
        pintura.Shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(PlanoCarta.Ancho, PlanoCarta.Alto),
            [SKColor.Parse(degradado.Desde), SKColor.Parse(degradado.Hasta)],
            SKShaderTileMode.Clamp);
        lienzo.DrawRect(new SKRect(0, 0, PlanoCarta.Ancho, PlanoCarta.Alto), pintura);
    }

    private static void Figura(SKCanvas lienzo, SKColor color)
    {
        using var pintura = new SKPaint { Color = color.WithAlpha((byte)(255 * PlanoCarta.OpacidadFigura)), IsAntialias = true };
        var caja = PlanoCarta.Foto;
        lienzo.Save();
        lienzo.Translate(caja.X, caja.Y);
        lienzo.DrawCircle(PlanoCarta.Cabeza.X, PlanoCarta.Cabeza.Y, PlanoCarta.Cabeza.Radio, pintura);
        using var silueta = SKPath.ParseSvgPathData(PlanoCarta.Silueta);
        lienzo.DrawPath(silueta, pintura);
        lienzo.Restore();
    }

    private static void Textos(SKCanvas lienzo, DatosImagenCarta datos, SKPaint pintura)
    {
        if (datos.Valoracion is { } valoracion)
        {
            Centrado(lienzo, valoracion.ToString(), PlanoCarta.Valoracion, ExtraNegrita, PlanoCarta.LetraValoracion, pintura);
        }

        if (datos.Posicion is { } posicion)
        {
            Centrado(lienzo, PlanoCarta.Abreviatura(posicion), PlanoCarta.Posicion, Negrita, PlanoCarta.LetraPosicion, pintura);
        }

        if (datos.Dorsal is { } dorsal)
        {
            Centrado(lienzo, dorsal.ToString(), PlanoCarta.Dorsal, ExtraNegrita, PlanoCarta.LetraDorsal, pintura, SKTextAlign.Right);
        }

        var (lineas, tamano) = PlanoCarta.AjustarNombre(datos.Nombres, datos.Apellidos, Medir);
        var caja = PlanoCarta.Nombre;
        var altoLinea = caja.Alto / lineas.Count;
        for (var indice = 0; indice < lineas.Count; indice++)
        {
            Centrado(lienzo, lineas[indice], caja with { Y = caja.Y + (indice * altoLinea), Alto = altoLinea }, Negrita, tamano, pintura);
        }

        Estadisticas(lienzo, datos.Estadisticas, pintura);
        Centrado(lienzo, PlanoCarta.TextoDeRareza(datos.Rareza), PlanoCarta.Rareza, Negrita, PlanoCarta.LetraRareza, pintura);
    }

    private static void Estadisticas(SKCanvas lienzo, ValoresCartaDto? valores, SKPaint pintura)
    {
        int[]? numeros = valores is null ? null : [valores.Ritmo, valores.Tiro, valores.Pase, valores.Regate, valores.Defensa, valores.Fisico];
        for (var columna = 0; columna < 2; columna++)
        {
            for (var fila = 0; fila < 3; fila++)
            {
                var valor = numeros is null ? "–" : numeros[(columna * 3) + fila].ToString();
                var (x, ancho) = PlanoCarta.Columnas[columna];
                var caja = new Caja(x, PlanoCarta.Filas[fila], ancho, PlanoCarta.AltoFila);
                var anchoValor = Centrado(lienzo, valor, caja, ExtraNegrita, PlanoCarta.LetraValorEstadistica, pintura, SKTextAlign.Left);
                var sigla = caja with { X = x + anchoValor + PlanoCarta.SeparacionEstadistica };
                Centrado(lienzo, PlanoCarta.Siglas[columna][fila], sigla, Negrita, PlanoCarta.LetraSiglaEstadistica, pintura, SKTextAlign.Left);
            }
        }
    }

    // Escribe una línea centrada en vertical en la caja; devuelve su ancho.
    private static float Centrado(
        SKCanvas lienzo, string texto, Caja caja, SKTypeface letra, float tamano, SKPaint pintura, SKTextAlign alineacion = SKTextAlign.Center)
    {
        using var fuente = new SKFont(letra, tamano);
        var metricas = fuente.Metrics;
        var linea = caja.Y + (caja.Alto / 2) - ((metricas.Ascent + metricas.Descent) / 2);
        var x = alineacion switch
        {
            SKTextAlign.Left => caja.X,
            SKTextAlign.Right => caja.X + caja.Ancho,
            _ => caja.X + (caja.Ancho / 2),
        };
        lienzo.DrawText(texto, x, linea, alineacion, fuente, pintura);
        return fuente.MeasureText(texto);
    }

    private static float Medir(string texto, float tamano)
    {
        using var fuente = new SKFont(Negrita, tamano);
        return fuente.MeasureText(texto);
    }

    private static SKTypeface Cargar(string archivo)
    {
        var ensamblado = typeof(GeneradorImagenCarta).Assembly;
        var recurso = ensamblado.GetManifestResourceNames().Single(nombre => nombre.EndsWith(archivo, StringComparison.Ordinal));
        using var flujo = ensamblado.GetManifestResourceStream(recurso)!;
        return SKTypeface.FromStream(flujo) ?? throw new InvalidOperationException($"No se pudo leer la fuente {archivo}.");
    }
}
