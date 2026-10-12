using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace LFClub.Pruebas.Integracion.Carta;

/// <summary>
/// El generador dibuja un PNG de 750 × 1050 con y sin foto, escudo, colores y estadísticas, con las
/// esquinas transparentes y el marco del color de la rareza (spec 010, historia 4; contracts/carta.md).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class GeneradorImagenCartaPruebas
{
    private readonly IGeneradorImagenCarta _generador;

    public GeneradorImagenCartaPruebas(FabricaApi fabrica)
    {
        _generador = fabrica.Services.GetRequiredService<IGeneradorImagenCarta>();
    }

    private static DatosImagenCarta Completa(RarezaCarta rareza = RarezaCarta.ORO) => new(
        "Ana María",
        "Pérez Gómez",
        10,
        PosicionJugador.DELANTERO,
        new ValoresCartaDto(70, 65, 80, 72, 60, 88),
        73,
        rareza,
        SembradorCartas.Imagen(SKEncodedImageFormat.Jpeg, SKColors.SteelBlue, 80, 100),
        SembradorCartas.Imagen(SKEncodedImageFormat.Png, SKColors.Red, 60, 80),
        "#1B3A6B",
        "#F2C200");

    private static SKBitmap Decodificar(byte[] png)
    {
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], png.Take(4));
        return SKBitmap.Decode(png);
    }

    [Theory]
    [InlineData(RarezaCarta.BRONCE, "#B0793F")]
    [InlineData(RarezaCarta.PLATA, "#BFC5CC")]
    [InlineData(RarezaCarta.ORO, "#D9A62E")]
    public void Dibuja_un_png_con_las_esquinas_transparentes_y_el_marco_de_la_rareza(RarezaCarta rareza, string color)
    {
        using var imagen = Decodificar(_generador.Generar(Completa(rareza)));

        Assert.Equal((750, 1050), (imagen.Width, imagen.Height));
        Assert.Equal(0, imagen.GetPixel(0, 0).Alpha);
        Assert.Equal(0, imagen.GetPixel(749, 1049).Alpha);
        // Marco metálico: a media altura, el color de la rareza; arriba, más claro; abajo, más oscuro.
        var esperado = SKColor.Parse(color);
        var medio = imagen.GetPixel(9, 525);
        Assert.InRange(medio.Red, esperado.Red - 3, esperado.Red + 3);
        Assert.InRange(medio.Green, esperado.Green - 3, esperado.Green + 3);
        Assert.InRange(medio.Blue, esperado.Blue - 3, esperado.Blue + 3);
        Assert.True(imagen.GetPixel(300, 9).Green > medio.Green && imagen.GetPixel(375, 1040).Green < medio.Green);
    }

    [Fact]
    public void Sin_foto_ni_escudo_ni_colores_ni_estadisticas_tambien_dibuja()
    {
        var datos = new DatosImagenCarta("Beto", "Ríos", null, null, null, null, RarezaCarta.BRONCE, null, null, null, null);

        using var imagen = Decodificar(_generador.Generar(datos));

        Assert.Equal((750, 1050), (imagen.Width, imagen.Height));

        // Fondo neutro abajo a la derecha, fuera del panel y del reflejo: el acento neutro.
        Assert.InRange(imagen.GetPixel(725, 1020).Red, 0x5E - 20, 0x5E + 20);
    }

    [Fact]
    public void Una_imagen_que_no_se_puede_leer_deja_la_figura_generica() =>
        Assert.NotEmpty(_generador.Generar(Completa() with { Foto = [1, 2, 3], Escudo = [4, 5, 6] }));

    [Fact]
    public void Un_nombre_de_sesenta_caracteres_no_falla()
    {
        var datos = Completa() with { Nombres = "Juan Sebastián Alejandro", Apellidos = "Montoya de los Ríos Villanueva Echever" };

        using var imagen = Decodificar(_generador.Generar(datos));

        Assert.Equal(750, imagen.Width);
    }
}
