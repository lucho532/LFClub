using System.Text;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>Archivos de prueba para el escudo y la foto de perfil: basta la firma binaria.</summary>
public static class Imagenes
{
    public static byte[] Png(int relleno = 16) =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[relleno]];

    public static byte[] Jpeg(int relleno = 16) => [0xFF, 0xD8, 0xFF, 0xE0, .. new byte[relleno]];

    public static byte[] Webp(int relleno = 16) => [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, .. new byte[relleno]];

    public static byte[] Texto() => Encoding.UTF8.GetBytes("esto no es una imagen");

    public static byte[] Svg() => Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");

    /// <summary>Un PNG de un byte más de 1 MB.</summary>
    public static byte[] PngDeMasDe1Mb() => Png((1024 * 1024) - 7);
}
