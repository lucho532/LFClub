using LFClub.Aplicacion.Validadores;

namespace LFClub.Pruebas.Unitarias.Validadores;

public class ValidadorSoportePagoPruebas
{
    private static byte[] Png(int relleno) =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[relleno]];

    [Fact]
    public void Un_png_un_jpeg_y_un_webp_pasan()
    {
        Assert.Equal("image/png", ValidadorSoportePago.Validar(Png(8)).TipoContenido);
        Assert.Equal("image/jpeg", ValidadorSoportePago.Validar([0xFF, 0xD8, 0xFF, 0xE0, 0, 0]).TipoContenido);
        byte[] webp = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, 0, 0];
        Assert.Equal("image/webp", ValidadorSoportePago.Validar(webp).TipoContenido);
    }

    [Fact]
    public void Sin_contenido_es_requerido()
    {
        Assert.Equal(ValidadorSoportePago.CodigoRequerido, ValidadorSoportePago.Validar([]).Codigo);
    }

    [Fact]
    public void Un_pdf_o_un_texto_no_son_una_imagen()
    {
        Assert.Equal(ValidadorSoportePago.CodigoInvalido, ValidadorSoportePago.Validar("%PDF-1.4 x"u8).Codigo);
        Assert.Equal(ValidadorSoportePago.CodigoInvalido, ValidadorSoportePago.Validar("hola"u8).Codigo);
    }

    [Fact]
    public void Hasta_5_MB_pasa_y_un_byte_mas_no()
    {
        var limite = Png(ValidadorSoportePago.TamanoMaximo - 8);

        Assert.Null(ValidadorSoportePago.Validar(limite).Codigo);
        Assert.Equal(
            ValidadorSoportePago.CodigoDemasiadoGrande, ValidadorSoportePago.Validar([.. limite, 0]).Codigo);
    }
}
