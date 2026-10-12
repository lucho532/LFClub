using System.Text.RegularExpressions;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de los colores de un club.
/// Su responsabilidad es exigir los dos colores con el formato <c>#RRGGBB</c>.
/// No comprueba el contraste: ese aviso lo da el panel antes de guardar y no impide hacerlo
/// (RF-009, research §11).
/// </summary>
public static partial class ValidadorColores
{
    /// <summary>Lanza <c>datos_invalidos</c> si algún color falta o no tiene el formato.</summary>
    public static void Validar(ActualizarColoresDto datos)
    {
        var errores = new ErroresDeValidacion();
        ValidarColor(errores, "colorPrincipal", datos.ColorPrincipal);
        ValidarColor(errores, "colorAcento", datos.ColorAcento);
        errores.LanzarSiHayErrores();
    }

    private static void ValidarColor(ErroresDeValidacion errores, string campo, string? color)
    {
        if (color is null || !Formato().IsMatch(color))
        {
            errores.Agregar(campo, "Escribe el color con el formato #RRGGBB, por ejemplo #B8370F.");
        }
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex Formato();
}
