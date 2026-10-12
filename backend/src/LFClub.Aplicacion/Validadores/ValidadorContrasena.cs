using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de una contraseña nueva (research §5, supuesto 2).
/// Su responsabilidad es exigir entre 8 y 128 caracteres, sin reglas de composición.
/// No normaliza la contraseña (ni la recorta ni cambia sus mayúsculas) y no calcula su hash.
/// </summary>
public static class ValidadorContrasena
{
    /// <summary>Longitud mínima.</summary>
    public const int Minimo = 8;

    /// <summary>Longitud máxima.</summary>
    public const int Maximo = 128;

    /// <summary>Añade un error al campo si la contraseña no es válida.</summary>
    public static void Validar(ErroresDeValidacion errores, string campo, string? contrasena)
    {
        if (string.IsNullOrEmpty(contrasena))
        {
            errores.Agregar(campo, "La contraseña es obligatoria.");
        }
        else if (contrasena.Length is < Minimo or > Maximo)
        {
            errores.Agregar(campo, $"La contraseña debe tener entre {Minimo} y {Maximo} caracteres.");
        }
    }
}
