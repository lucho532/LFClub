namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de la imagen que la familia sube como soporte de una transferencia
/// (spec 015, RF-021).
/// Su responsabilidad es exigir una imagen PNG, JPEG o WebP, reconocida por su firma binaria, de
/// hasta 5 MB, y decir qué tipo de contenido es. Reutiliza la detección de <see cref="ValidadorImagen"/>.
/// No confía en la extensión ni en el tipo que declara el navegador, no admite PDF ni SVG, y no guarda
/// la imagen.
/// </summary>
public static class ValidadorSoportePago
{
    /// <summary>Tamaño máximo admitido: 5 MB.</summary>
    public const int TamanoMaximo = 5 * 1024 * 1024;

    /// <summary>Código cuando no se envió ninguna imagen.</summary>
    public const string CodigoRequerido = "soporte_requerido";

    /// <summary>Código cuando el archivo no es una imagen admitida.</summary>
    public const string CodigoInvalido = "soporte_invalido";

    /// <summary>Código cuando la imagen supera el tamaño máximo.</summary>
    public const string CodigoDemasiadoGrande = "soporte_demasiado_grande";

    /// <summary>
    /// Valida el contenido. Si es válido devuelve su tipo de contenido y ningún código; si no, el
    /// código del rechazo.
    /// </summary>
    public static (string? TipoContenido, string? Codigo) Validar(ReadOnlySpan<byte> contenido)
    {
        if (contenido.Length == 0)
        {
            return (null, CodigoRequerido);
        }

        if (contenido.Length > TamanoMaximo)
        {
            return (null, CodigoDemasiadoGrande);
        }

        var tipo = ValidadorImagen.DetectarTipo(contenido);
        return tipo is null ? (null, CodigoInvalido) : (tipo, null);
    }
}
