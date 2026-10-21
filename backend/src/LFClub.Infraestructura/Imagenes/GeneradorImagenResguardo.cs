using LFClub.Aplicacion.Interfaces;
using SkiaSharp;

namespace LFClub.Infraestructura.Imagenes;

/// <summary>
/// Representa el dibujo del resguardo de pago como PNG de 1080 × 1350 con SkiaSharp (spec 015, RF-025;
/// research §14), siguiendo <see cref="PlanoResguardo"/>.
/// Su responsabilidad es pintar, de atrás hacia delante, el degradado del club con las esquinas
/// redondeadas, su escudo y su nombre (con el color de texto de <see cref="ContrasteDeColor"/>), un panel
/// claro con el título y el número del recibo, el valor, el jugador (sin su documento), el medio y la
/// entidad, la fecha, lo que cubre, quién confirmó y "Al día" o el saldo pendiente, y, si el pago se
/// anuló, el sello ANULADO en diagonal. Usa Barlow Condensed de los recursos incrustados, como la carta.
/// No decide quién lo ve, no consulta nada, no guarda la imagen y no añade ningún dato que no reciba.
/// </summary>
public sealed class GeneradorImagenResguardo : IGeneradorImagenResguardo
{
    private static readonly SKTypeface Negrita = Cargar("BarlowCondensed-Bold.ttf");
    private static readonly SKTypeface ExtraNegrita = Cargar("BarlowCondensed-ExtraBold.ttf");

    /// <inheritdoc />
    public byte[] Generar(DatosImagenResguardo datos)
    {
        var info = new SKImageInfo(PlanoResguardo.Ancho, PlanoResguardo.Alto, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var superficie = SKSurface.Create(info);
        var lienzo = superficie.Canvas;
        lienzo.Clear(SKColors.Transparent);

        var degradado = ContrasteDeColor.Degradado(datos.ColorPrincipal, datos.ColorAcento);
        var borde = new SKRoundRect(SKRect.Create(PlanoResguardo.Ancho, PlanoResguardo.Alto), PlanoResguardo.Radio);
        lienzo.Save();
        lienzo.ClipRoundRect(borde, SKClipOperation.Intersect, true);
        Fondo(lienzo, degradado);
        Encabezado(lienzo, datos, degradado);
        Panel(lienzo);
        Recibo(lienzo, datos);
        if (datos.Anulado)
        {
            Sello(lienzo);
        }

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
            new SKPoint(PlanoResguardo.Ancho, PlanoResguardo.Alto),
            [SKColor.Parse(degradado.Desde), SKColor.Parse(degradado.Hasta)],
            SKShaderTileMode.Clamp);
        lienzo.DrawRect(SKRect.Create(PlanoResguardo.Ancho, PlanoResguardo.Alto), pintura);
    }

    private static void Encabezado(SKCanvas lienzo, DatosImagenResguardo datos, (string Desde, string Hasta) degradado)
    {
        var conEscudo = datos.Escudo is { Length: > 0 };
        CapasCarta.Escudo(lienzo, datos.Escudo, PlanoResguardo.Escudo);
        var cajaDelNombre = conEscudo ? PlanoResguardo.NombreDelClub : PlanoResguardo.NombreDelClubSinEscudo;

        var (texto, sombra) = ContrasteDeColor.ColorDelTexto(degradado);
        using var pintura = new SKPaint { Color = SKColor.Parse(texto), IsAntialias = true };
        if (sombra is not null)
        {
            // Halo como el de la carta, para que el nombre se lea sobre cualquier color del club.
            var halo = SKColor.Parse(sombra).WithAlpha(200);
            pintura.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 3, 3, halo);
        }

        Linea(lienzo, Recortar(datos.ClubNombre, Negrita, PlanoResguardo.LetraClub, cajaDelNombre.Ancho),
            cajaDelNombre, Negrita, PlanoResguardo.LetraClub, pintura, SKTextAlign.Left);
    }

    private static void Panel(SKCanvas lienzo)
    {
        using var pintura = new SKPaint { Color = SKColor.Parse(PlanoResguardo.ColorDelPanel), IsAntialias = true };
        var caja = PlanoResguardo.Panel;
        lienzo.DrawRoundRect(
            SKRect.Create(caja.X, caja.Y, caja.Ancho, caja.Alto), PlanoResguardo.RadioDelPanel, PlanoResguardo.RadioDelPanel, pintura);
    }

    private static void Recibo(SKCanvas lienzo, DatosImagenResguardo datos)
    {
        using var texto = new SKPaint { Color = SKColor.Parse(PlanoResguardo.ColorDelTexto), IsAntialias = true };
        using var etiqueta = new SKPaint { Color = SKColor.Parse(PlanoResguardo.ColorDeLaEtiqueta), IsAntialias = true };

        Linea(lienzo, PlanoResguardo.TextoDelTitulo(datos.NumeroRecibo), PlanoResguardo.Titulo, Negrita,
            PlanoResguardo.LetraTitulo, etiqueta, SKTextAlign.Left);
        Linea(lienzo, PlanoResguardo.Pesos(datos.Valor), PlanoResguardo.Valor, ExtraNegrita,
            PlanoResguardo.LetraValor, texto, SKTextAlign.Left);

        var filas = new (string Etiqueta, string Valor)[]
        {
            ("Jugador", datos.JugadorNombre),
            ("Medio de pago", PlanoResguardo.TextoDelMedio(datos.Medio, datos.Entidad)),
            ("Fecha del pago", PlanoResguardo.TextoDeLaFecha(datos.Fecha)),
            ("Cubre", string.IsNullOrWhiteSpace(datos.Concepto) ? "—" : datos.Concepto),
            ("Confirmado por", datos.ConfirmadoPorNombre),
            ("Estado de cuenta", PlanoResguardo.TextoDelSaldo(datos.SaldoTrasPago)),
        };

        for (var indice = 0; indice < filas.Length; indice++)
        {
            var (y, alto) = PlanoResguardo.Filas[indice];
            var (rotulo, valor) = filas[indice];
            Linea(lienzo, rotulo, new Caja(PlanoResguardo.MargenDeFila, y, PlanoResguardo.AnchoDeFila, 40), Negrita,
                PlanoResguardo.LetraEtiqueta, etiqueta, SKTextAlign.Left);

            var lineas = Ajustar(valor, Negrita, PlanoResguardo.LetraFila, PlanoResguardo.AnchoDeFila, indice == 3 ? PlanoResguardo.LineasDeConcepto : 1);
            var altoDeLinea = (alto - 44) / Math.Max(1, indice == 3 ? PlanoResguardo.LineasDeConcepto : 1);
            for (var linea = 0; linea < lineas.Count; linea++)
            {
                Linea(lienzo, lineas[linea],
                    new Caja(PlanoResguardo.MargenDeFila, y + 44 + (linea * altoDeLinea), PlanoResguardo.AnchoDeFila, altoDeLinea),
                    Negrita, PlanoResguardo.LetraFila, texto, SKTextAlign.Left);
            }
        }
    }

    private static void Sello(SKCanvas lienzo)
    {
        var color = SKColor.Parse(PlanoResguardo.ColorDelSello).WithAlpha((byte)(255 * PlanoResguardo.OpacidadDelSello));
        using var letra = new SKPaint { Color = color, IsAntialias = true };
        using var contorno = new SKPaint { Color = color, IsAntialias = true, IsStroke = true, StrokeWidth = 14 };
        using var fuente = new SKFont(ExtraNegrita, PlanoResguardo.LetraDelSello);
        var ancho = fuente.MeasureText("ANULADO");
        var metricas = fuente.Metrics;

        lienzo.Save();
        lienzo.Translate(PlanoResguardo.Ancho / 2f, PlanoResguardo.Alto / 2f);
        lienzo.RotateDegrees(PlanoResguardo.AnguloDelSello);
        var marco = SKRect.Create(-(ancho / 2) - 40, metricas.Ascent - 30, ancho + 80, metricas.Descent - metricas.Ascent + 60);
        lienzo.DrawRoundRect(marco, 24, 24, contorno);
        lienzo.DrawText("ANULADO", 0, -((metricas.Ascent + metricas.Descent) / 2), SKTextAlign.Center, fuente, letra);
        lienzo.Restore();
    }

    // Escribe una línea centrada en vertical en la caja.
    private static void Linea(SKCanvas lienzo, string texto, Caja caja, SKTypeface letra, float tamano, SKPaint pintura, SKTextAlign alineacion)
    {
        using var fuente = new SKFont(letra, tamano);
        var metricas = fuente.Metrics;
        var linea = caja.Y + (caja.Alto / 2) - ((metricas.Ascent + metricas.Descent) / 2);
        var x = alineacion == SKTextAlign.Left ? caja.X : caja.X + (caja.Ancho / 2);
        lienzo.DrawText(texto, x, linea, alineacion, fuente, pintura);
    }

    // Parte el texto en hasta `maximo` líneas del ancho dado; lo que no cabe se corta con puntos suspensivos.
    private static List<string> Ajustar(string texto, SKTypeface letra, float tamano, float ancho, int maximo)
    {
        using var fuente = new SKFont(letra, tamano);
        var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lineas = new List<string>();
        var inicio = 0;
        while (inicio < palabras.Length)
        {
            if (lineas.Count == maximo - 1)
            {
                lineas.Add(Recortar(string.Join(' ', palabras[inicio..]), letra, tamano, ancho));
                break;
            }

            var fin = inicio + 1;
            while (fin < palabras.Length && fuente.MeasureText(string.Join(' ', palabras[inicio..(fin + 1)])) <= ancho)
            {
                fin++;
            }

            lineas.Add(Recortar(string.Join(' ', palabras[inicio..fin]), letra, tamano, ancho));
            inicio = fin;
        }

        return lineas.Count == 0 ? [string.Empty] : lineas;
    }

    private static string Recortar(string texto, SKTypeface letra, float tamano, float ancho)
    {
        using var fuente = new SKFont(letra, tamano);
        if (fuente.MeasureText(texto) <= ancho)
        {
            return texto;
        }

        while (texto.Length > 1 && fuente.MeasureText(texto + "…") > ancho)
        {
            texto = texto[..^1];
        }

        return texto.TrimEnd() + "…";
    }

    private static SKTypeface Cargar(string archivo)
    {
        var ensamblado = typeof(GeneradorImagenResguardo).Assembly;
        var recurso = ensamblado.GetManifestResourceNames().Single(nombre => nombre.EndsWith(archivo, StringComparison.Ordinal));
        using var flujo = ensamblado.GetManifestResourceStream(recurso)!;
        return SKTypeface.FromStream(flujo) ?? throw new InvalidOperationException($"No se pudo leer la fuente {archivo}.");
    }
}
