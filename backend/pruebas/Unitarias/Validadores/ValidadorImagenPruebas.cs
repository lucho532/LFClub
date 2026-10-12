using System.Text;
using LFClub.Aplicacion.Validadores;

namespace LFClub.Pruebas.Unitarias.Validadores;

public class ValidadorImagenPruebas
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0];
    private static readonly byte[] Webp = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, 0, 0];

    [Fact]
    public void Acepta_png() => Assert.Equal(("image/png", null), ValidadorImagen.Validar(Png));

    [Fact]
    public void Acepta_jpeg() => Assert.Equal(("image/jpeg", null), ValidadorImagen.Validar(Jpeg));

    [Fact]
    public void Acepta_webp() => Assert.Equal(("image/webp", null), ValidadorImagen.Validar(Webp));

    [Fact]
    public void Rechaza_un_archivo_que_no_es_imagen() =>
        Assert.Equal((null, MotivoRechazoImagen.NoEsImagen), ValidadorImagen.Validar("hola, soy un texto"u8));

    [Fact]
    public void Rechaza_un_svg() =>
        Assert.Equal(
            (null, MotivoRechazoImagen.NoEsImagen),
            ValidadorImagen.Validar(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>")));

    [Fact]
    public void Rechaza_un_riff_que_no_es_webp() =>
        Assert.Equal(
            (null, MotivoRechazoImagen.NoEsImagen),
            ValidadorImagen.Validar([.. "RIFF"u8, 0, 0, 0, 0, .. "WAVE"u8]));

    [Fact]
    public void Rechaza_un_archivo_vacio() =>
        Assert.Equal((null, MotivoRechazoImagen.NoEsImagen), ValidadorImagen.Validar([]));

    [Fact]
    public void Acepta_exactamente_1_mb()
    {
        var contenido = new byte[ValidadorImagen.TamanoMaximo];
        Png.CopyTo(contenido, 0);

        Assert.Equal(("image/png", null), ValidadorImagen.Validar(contenido));
    }

    [Fact]
    public void Rechaza_mas_de_1_mb()
    {
        var contenido = new byte[ValidadorImagen.TamanoMaximo + 1];
        Png.CopyTo(contenido, 0);

        Assert.Equal((null, MotivoRechazoImagen.DemasiadoGrande), ValidadorImagen.Validar(contenido));
    }
}
