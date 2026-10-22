using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Imagenes;
using SkiaSharp;

namespace LFClub.Pruebas.Unitarias.Imagenes;

public class PlanoResguardoPruebas
{
    private static DatosImagenResguardo Datos(
        string? principal = "#0B3D91", string? acento = "#FFD500", bool anulado = false, string? concepto = "Mensualidad octubre de 2026") => new(
        "Valfor F.C.", null, principal, acento, 7, "Ana Arango", 65000m, MedioPago.TRANSFERENCIA, "Nequi",
        new DateOnly(2026, 10, 13), concepto, "Pat Presidente", 0m, anulado);

    private static SKBitmap Dibujar(DatosImagenResguardo datos)
    {
        var bytes = new GeneradorImagenResguardo().Generar(datos);
        return SKBitmap.Decode(bytes);
    }

    [Fact]
    public void El_lienzo_mide_1080_por_1350_y_el_png_sale_con_esas_medidas()
    {
        using var imagen = Dibujar(Datos());

        Assert.Equal(PlanoResguardo.Ancho, imagen.Width);
        Assert.Equal(PlanoResguardo.Alto, imagen.Height);
        Assert.Equal((1080, 1350), (PlanoResguardo.Ancho, PlanoResguardo.Alto));
    }

    [Fact]
    public void Todas_las_cajas_caben_dentro_del_lienzo_y_las_filas_dentro_del_panel()
    {
        var panel = PlanoResguardo.Panel;
        Assert.InRange(panel.X + panel.Ancho, 0, PlanoResguardo.Ancho);
        Assert.InRange(panel.Y + panel.Alto, 0, PlanoResguardo.Alto);
        foreach (var (y, alto) in PlanoResguardo.Filas)
        {
            Assert.InRange(y, panel.Y, panel.Y + panel.Alto);
            Assert.InRange(y + alto, panel.Y, panel.Y + panel.Alto);
        }

        Assert.True(PlanoResguardo.MargenDeFila + PlanoResguardo.AnchoDeFila <= panel.X + panel.Ancho);
    }

    [Fact]
    public void Las_filas_no_se_pisan()
    {
        var filas = PlanoResguardo.Filas;
        for (var indice = 1; indice < filas.Length; indice++)
        {
            Assert.True(filas[indice].Y >= filas[indice - 1].Y + filas[indice - 1].Alto);
        }
    }

    [Theory]
    [InlineData(PlanoResguardo.ColorDelTexto)]
    [InlineData(PlanoResguardo.ColorDeLaEtiqueta)]
    public void El_texto_del_panel_tiene_contraste_de_lectura_sobre_el_panel_con_cualquier_color_de_club(string color)
    {
        Assert.True(ContrasteDeColor.Contraste(color, PlanoResguardo.ColorDelPanel) >= 4.5);
    }

    [Theory]
    [InlineData("#0B3D91", "#FFD500")]
    [InlineData("#FFD500", null)]
    [InlineData("#000000", "#000000")]
    [InlineData("#FFFFFF", "#FFFFFF")]
    [InlineData(null, null)]
    public void El_encabezado_elige_el_texto_con_mejor_contraste_o_le_pone_halo(string? principal, string? acento)
    {
        var degradado = ContrasteDeColor.Degradado(principal, acento);

        var (texto, sombra) = ContrasteDeColor.ColorDelTexto(degradado);

        var peor = Math.Min(ContrasteDeColor.Contraste(texto, degradado.Desde), ContrasteDeColor.Contraste(texto, degradado.Hasta));
        Assert.True(peor >= ContrasteDeColor.ContrasteSinSombra || sombra is not null);
    }

    [Fact]
    public void El_sello_anulado_cambia_la_imagen()
    {
        using var normal = Dibujar(Datos(anulado: false));
        using var anulado = Dibujar(Datos(anulado: true));

        Assert.NotEqual(normal.Bytes, anulado.Bytes);
    }

    [Fact]
    public void Un_club_sin_colores_ni_escudo_y_un_concepto_larguisimo_se_dibujan_sin_error()
    {
        using var imagen = Dibujar(Datos(null, null, concepto: string.Join("; ", Enumerable.Repeat("Mensualidad octubre de 2026", 30))));

        Assert.Equal(PlanoResguardo.Ancho, imagen.Width);
    }

    [Fact]
    public void Los_textos_usan_pesos_con_puntos_de_miles_y_fechas_en_espanol()
    {
        Assert.Equal("$ 65.000", PlanoResguardo.Pesos(65000m));
        Assert.Equal("$ 1.250.000", PlanoResguardo.Pesos(1250000m));
        Assert.Equal("13 de octubre de 2026", PlanoResguardo.TextoDeLaFecha(new DateOnly(2026, 10, 13)));
        Assert.Equal("Al día", PlanoResguardo.TextoDelSaldo(0m));
        Assert.Equal("Saldo pendiente: $ 40.000", PlanoResguardo.TextoDelSaldo(40000m));
        Assert.Equal("Transferencia (Nequi)", PlanoResguardo.TextoDelMedio(MedioPago.TRANSFERENCIA, "Nequi"));
        Assert.Equal("Efectivo", PlanoResguardo.TextoDelMedio(MedioPago.EFECTIVO, null));
        Assert.Equal("RESGUARDO DE PAGO N.º 7", PlanoResguardo.TextoDelTitulo(7));
    }
}
