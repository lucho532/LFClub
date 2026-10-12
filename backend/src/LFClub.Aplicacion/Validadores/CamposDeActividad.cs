using System.Globalization;
using System.Text.RegularExpressions;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la lectura de los campos que comparten los formularios de entrenamientos, partidos y
/// calendario (spec 009): fechas <c>AAAA-MM-DD</c>, horas <c>HH:mm</c> y la lista de categorías.
/// Su responsabilidad es convertir cada texto y, si no es válido, añadir el error en su campo con
/// un mensaje en español.
/// No decide qué campos son obligatorios en cada formulario ni comprueba que las categorías existan.
/// </summary>
public static partial class CamposDeActividad
{
    /// <summary>Lee una fecha obligatoria; nula si falta o no es válida, con el error añadido.</summary>
    public static DateOnly? Fecha(ErroresDeValidacion errores, string campo, string? texto, string nombreVisible)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            errores.Agregar(campo, $"{nombreVisible} es obligatoria.");
            return null;
        }

        if (!DateOnly.TryParseExact(texto.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            errores.Agregar(campo, $"{nombreVisible} no es válida.");
            return null;
        }

        return fecha;
    }

    /// <summary>Lee una hora obligatoria <c>HH:mm</c>; nula si falta o no es válida, con el error añadido.</summary>
    public static TimeOnly? Hora(ErroresDeValidacion errores, string campo, string? texto, string nombreVisible)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            errores.Agregar(campo, $"{nombreVisible} es obligatoria.");
            return null;
        }

        if (!FormatoHora().IsMatch(texto.Trim()))
        {
            errores.Agregar(campo, $"{nombreVisible} no es válida.");
            return null;
        }

        return TimeOnly.ParseExact(texto.Trim(), "HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>Exige una o más categorías sin repetir en el campo <c>categoriaIds</c>.</summary>
    public static IReadOnlyList<Guid> Categorias(ErroresDeValidacion errores, IReadOnlyList<Guid>? categoriaIds)
    {
        if (categoriaIds is null || categoriaIds.Count == 0)
        {
            errores.Agregar("categoriaIds", "Elige al menos una categoría.");
            return [];
        }

        if (categoriaIds.Distinct().Count() != categoriaIds.Count)
        {
            errores.Agregar("categoriaIds", "Hay una categoría repetida.");
        }

        return categoriaIds.Distinct().ToList();
    }

    /// <summary>Un texto sin espacios a los lados, o nulo si queda vacío.</summary>
    public static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    [GeneratedRegex("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    private static partial Regex FormatoHora();
}
