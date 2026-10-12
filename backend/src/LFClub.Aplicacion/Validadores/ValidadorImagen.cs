namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de las imágenes que se cargan: el escudo de un club y la foto de
/// perfil de una cuenta (research §10 y §16).
/// Su responsabilidad es aceptar solo PNG, JPEG o WebP reconocidos por su firma binaria, de hasta
/// 1 MB, y decir qué tipo de contenido es. Su detección de firmas la reutiliza la validación de
/// los archivos de la ficha del jugador, que admite además PDF y tiene su propio límite.
/// No confía en la extensión ni en la cabecera enviada, no admite SVG y no guarda la imagen.
/// </summary>
public static class ValidadorImagen
{
    /// <summary>Tamaño máximo admitido: 1 MB.</summary>
    public const int TamanoMaximo = 1024 * 1024;

    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] FirmaJpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FirmaRiff = "RIFF"u8.ToArray();
    private static readonly byte[] FirmaWebp = "WEBP"u8.ToArray();

    /// <summary>
    /// Valida el contenido. Si es válido devuelve su tipo de contenido y ningún motivo; si no, el
    /// motivo del rechazo.
    /// </summary>
    public static (string? TipoContenido, MotivoRechazoImagen? Motivo) Validar(ReadOnlySpan<byte> contenido)
    {
        if (contenido.Length > TamanoMaximo)
        {
            return (null, MotivoRechazoImagen.DemasiadoGrande);
        }

        var tipo = DetectarTipo(contenido);
        return tipo is null ? (null, MotivoRechazoImagen.NoEsImagen) : (tipo, null);
    }

    /// <summary>
    /// El tipo de contenido que delata la firma binaria del archivo: <c>image/png</c>,
    /// <c>image/jpeg</c> o <c>image/webp</c>; nulo si no es ninguna de esas imágenes. No mira el
    /// tamaño: lo usa también la validación de los archivos de la ficha, que tiene otro límite.
    /// </summary>
    public static string? DetectarTipo(ReadOnlySpan<byte> contenido)
    {
        if (contenido.StartsWith(FirmaPng))
        {
            return "image/png";
        }

        if (contenido.StartsWith(FirmaJpeg))
        {
            return "image/jpeg";
        }

        if (contenido.Length >= 12 && contenido.StartsWith(FirmaRiff) && contenido[8..12].SequenceEqual(FirmaWebp))
        {
            return "image/webp";
        }

        return null;
    }
}
