using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa el único lugar donde se normalizan los textos que identifican algo: correo, nombre
/// de club, documento e identificador de inicio de sesión (RF-002), y los nombres de archivo que se
/// descargan (spec 010).
/// Su responsabilidad es que lo guardado y lo escrito al buscar coincidan siempre.
/// No normaliza contraseñas (recortarlas o pasarlas a minúsculas las debilitaría) ni valida formatos.
/// </summary>
public static partial class NormalizadorTexto
{
    /// <summary>Correo sin espacios en los extremos y en minúsculas.</summary>
    public static string Correo(string? correo) => (correo ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Nombre de club en minúsculas, sin espacios en los extremos ni repetidos.</summary>
    public static string NombreClub(string? nombre) => SinEspaciosSobrantes(nombre).ToLowerInvariant();

    /// <summary>Texto para mostrar: sin espacios en los extremos ni repetidos, con sus mayúsculas.</summary>
    public static string SinEspaciosSobrantes(string? texto) =>
        Espacios().Replace((texto ?? string.Empty).Trim(), " ");

    /// <summary>Número de documento sin espacios ni puntos y en minúsculas.</summary>
    public static string Documento(string? documento) =>
        EspaciosYPuntos().Replace(documento ?? string.Empty, string.Empty).ToLowerInvariant();

    /// <summary>Identificador de inicio de sesión sin espacios en los extremos y en minúsculas.</summary>
    public static string Identificador(string? identificador) =>
        (identificador ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>
    /// Parte de un nombre de archivo: sin tildes, en minúsculas, con guiones en lugar de espacios y
    /// solo letras de la a a la z, números y guiones (spec 010, nombre de la imagen de la carta).
    /// </summary>
    public static string ParaArchivo(string? texto)
    {
        var sinTildes = string.Concat(SinEspaciosSobrantes(texto).Normalize(NormalizationForm.FormD)
            .Where(letra => CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark));
        return NoPermitidos().Replace(sinTildes.ToLowerInvariant().Replace(' ', '-'), string.Empty);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();

    [GeneratedRegex(@"[\s.]+")]
    private static partial Regex EspaciosYPuntos();

    [GeneratedRegex("[^a-z0-9-]")]
    private static partial Regex NoPermitidos();
}
