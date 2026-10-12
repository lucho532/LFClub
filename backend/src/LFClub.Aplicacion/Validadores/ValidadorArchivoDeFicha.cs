namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de los archivos que se entregan para los documentos de la ficha de un
/// jugador (RF-030).
/// Su responsabilidad es aceptar solo un PDF o una imagen JPEG, PNG o WebP, reconocidos por su
/// firma binaria, de hasta 10 MB, y decir qué tipo de contenido es.
/// No confía en la extensión ni en el tipo que declara el navegador, no abre el archivo para
/// comprobar que está bien formado y no lo guarda. Las imágenes las reconoce
/// <see cref="ValidadorImagen"/>, que conserva su propio límite para el escudo y la foto.
/// </summary>
public static class ValidadorArchivoDeFicha
{
    /// <summary>Tamaño máximo admitido: 10 MB.</summary>
    public const int TamanoMaximo = 10 * 1024 * 1024;

    /// <summary>Tipo de contenido de un PDF.</summary>
    public const string TipoPdf = "application/pdf";

    private static readonly byte[] FirmaPdf = "%PDF-"u8.ToArray();

    /// <summary>
    /// Valida el contenido. Si es válido devuelve su tipo de contenido y ningún motivo; si no, el
    /// motivo del rechazo.
    /// </summary>
    public static (string? TipoContenido, MotivoRechazoArchivo? Motivo) Validar(ReadOnlySpan<byte> contenido)
    {
        if (contenido.Length > TamanoMaximo)
        {
            return (null, MotivoRechazoArchivo.DemasiadoGrande);
        }

        var tipo = contenido.StartsWith(FirmaPdf) ? TipoPdf : ValidadorImagen.DetectarTipo(contenido);
        return tipo is null ? (null, MotivoRechazoArchivo.NoAdmitido) : (tipo, null);
    }
}
