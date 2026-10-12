using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de los datos editables de un club.
/// Su responsabilidad es exigir el nombre (hasta 120 caracteres) y limitar la sede (120), la
/// dirección (200), el correo de contacto (formato de correo, 254) y el teléfono (20), que son
/// opcionales.
/// No comprueba que el nombre esté libre: eso lo decide el índice único al guardar.
/// </summary>
public static class ValidadorConfiguracionClub
{
    /// <summary>Lanza <c>datos_invalidos</c> si los datos no son válidos.</summary>
    public static void Validar(ActualizarConfiguracionClubDto datos)
    {
        var errores = new ErroresDeValidacion();
        errores.Obligatorio("nombre", datos.Nombre, 120, "El nombre del club");
        errores.Maximo("sede", datos.Sede, 120, "La sede");
        errores.Maximo("direccion", datos.Direccion, 200, "La dirección");
        errores.Maximo("telefonoContacto", datos.TelefonoContacto, 20, "El teléfono de contacto");
        ValidadorCorreo.Opcional(errores, "correoContacto", datos.CorreoContacto);
        errores.LanzarSiHayErrores();
    }
}
