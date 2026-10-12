using System.Text.RegularExpressions;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación del formato de un correo electrónico.
/// Su responsabilidad es comprobar que el texto parece un correo y no pasa de 254 caracteres.
/// No comprueba que el buzón exista ni normaliza el correo.
/// </summary>
public static partial class ValidadorCorreo
{
    /// <summary>Longitud máxima de un correo.</summary>
    public const int Maximo = 254;

    /// <summary>Indica si el texto tiene formato de correo.</summary>
    public static bool EsValido(string? correo) =>
        correo is not null && correo.Trim().Length <= Maximo && Formato().IsMatch(correo.Trim());

    /// <summary>Exige un correo obligatorio y con formato; devuelve si es válido.</summary>
    public static bool Obligatorio(ErroresDeValidacion errores, string campo, string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            errores.Agregar(campo, "El correo es obligatorio.");
            return false;
        }

        return Opcional(errores, campo, correo);
    }

    /// <summary>Exige formato de correo solo si hay valor; devuelve si es válido.</summary>
    public static bool Opcional(ErroresDeValidacion errores, string campo, string? correo)
    {
        if (!string.IsNullOrWhiteSpace(correo) && !EsValido(correo))
        {
            errores.Agregar(campo, "Escribe un correo válido, como nombre@ejemplo.com.");
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$")]
    private static partial Regex Formato();
}
