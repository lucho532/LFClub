using System.Text;
using LFClub.Aplicacion.Validadores;

namespace LFClub.Pruebas.Unitarias.Validadores;

/// <summary>
/// Los archivos de la ficha se reconocen por su firma binaria, nunca por su nombre, y admiten
/// hasta 10 MB (RF-030; supuesto 5 del plan).
/// </summary>
public class ValidadorArchivoDeFichaPruebas
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];
    private static readonly byte[] Webp = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, 1, 2, 3];

    private static byte[] Pdf(int tamano)
    {
        var contenido = new byte[tamano];
        "%PDF-"u8.CopyTo(contenido);
        return contenido;
    }

    [Fact]
    public void Cada_firma_se_reconoce_con_su_tipo_de_contenido()
    {
        Assert.Equal(("application/pdf", null), ValidadorArchivoDeFicha.Validar(Pdf(32)));
        Assert.Equal(("image/png", null), ValidadorArchivoDeFicha.Validar(Png));
        Assert.Equal(("image/jpeg", null), ValidadorArchivoDeFicha.Validar(Jpeg));
        Assert.Equal(("image/webp", null), ValidadorArchivoDeFicha.Validar(Webp));
    }

    [Theory]
    [InlineData("esto no es un PDF, aunque se llame documento.pdf")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>")]
    [InlineData("PDF-1.7 sin el signo de porcentaje")]
    [InlineData(" %PDF-1.7 con un espacio delante")]
    public void Un_texto_disfrazado_no_se_admite(string texto) =>
        Assert.Equal((null, MotivoRechazoArchivo.NoAdmitido), ValidadorArchivoDeFicha.Validar(Encoding.UTF8.GetBytes(texto)));

    [Fact]
    public void Un_archivo_vacio_no_se_admite() =>
        Assert.Equal((null, MotivoRechazoArchivo.NoAdmitido), ValidadorArchivoDeFicha.Validar([]));

    [Fact]
    public void Admite_exactamente_10_MB_y_rechaza_un_byte_mas()
    {
        Assert.Equal(10 * 1024 * 1024, ValidadorArchivoDeFicha.TamanoMaximo);
        Assert.Equal(("application/pdf", null), ValidadorArchivoDeFicha.Validar(Pdf(ValidadorArchivoDeFicha.TamanoMaximo)));
        Assert.Equal(
            (null, MotivoRechazoArchivo.DemasiadoGrande),
            ValidadorArchivoDeFicha.Validar(Pdf(ValidadorArchivoDeFicha.TamanoMaximo + 1)));
    }

    // El tamaño se mira antes que el formato: un archivo enorme de otro tipo se rechaza por grande.
    [Fact]
    public void Un_archivo_demasiado_grande_de_otro_formato_se_rechaza_por_su_tamano() =>
        Assert.Equal(
            (null, MotivoRechazoArchivo.DemasiadoGrande),
            ValidadorArchivoDeFicha.Validar(new byte[ValidadorArchivoDeFicha.TamanoMaximo + 1]));

    // El escudo y la foto de perfil conservan su límite de 1 MB.
    [Fact]
    public void El_limite_de_las_imagenes_del_escudo_y_de_la_foto_no_cambia() =>
        Assert.Equal(1024 * 1024, ValidadorImagen.TamanoMaximo);
}
