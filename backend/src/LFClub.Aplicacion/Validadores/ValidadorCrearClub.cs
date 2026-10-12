using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de los datos para crear un club.
/// Su responsabilidad es exigir el nombre (hasta 120 caracteres) y el correo del presidente, con
/// formato de correo: no se puede crear un club sin presidente (constitución §12.5).
/// No comprueba que el nombre esté libre ni de quién es el correo: eso lo hace el servicio.
/// </summary>
public static class ValidadorCrearClub
{
    /// <summary>Lanza <c>datos_invalidos</c> si los datos no son válidos.</summary>
    public static void Validar(CrearClubDto datos)
    {
        var errores = new ErroresDeValidacion();
        errores.Obligatorio("nombre", datos.Nombre, 120, "El nombre del club");
        ValidadorCorreo.Obligatorio(errores, "correoPresidente", datos.CorreoPresidente);
        errores.LanzarSiHayErrores();
    }
}
