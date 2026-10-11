using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Utilidades;

namespace LaPecosa.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de los datos propios que se cambian desde "Mi perfil" (spec 008, RF-016).
/// Su responsabilidad es aplicar las mismas reglas y mensajes que el registro (§12.1): nombres y
/// apellidos obligatorios, de hasta 80 caracteres; fecha de nacimiento obligatoria y no futura; y
/// celular obligatorio, de hasta 20 caracteres. Reutiliza <see cref="ValidadorIdentidad"/>.
/// No valida documento ni correo, que no se cambian por aquí, y no comprueba qué roles tiene la
/// cuenta.
/// </summary>
public static class ValidadorDatosPropios
{
    /// <summary>Longitud máxima del celular, la misma del registro.</summary>
    public const int MaximoCelular = 20;

    /// <summary>
    /// Lanza <c>datos_invalidos</c> con los errores por campo si algo no es válido.
    /// <paramref name="hoy"/> es la fecha UTC del servidor, para rechazar fechas futuras.
    /// </summary>
    public static void Validar(CambiarDatosPropiosDto datos, DateOnly hoy)
    {
        var errores = new ErroresDeValidacion();

        ValidadorIdentidad.NombresYApellidos(errores, datos.Nombres, datos.Apellidos);
        ValidadorIdentidad.FechaNacimiento(errores, datos.FechaNacimiento, hoy);
        errores.Obligatorio("celular", datos.Celular, MaximoCelular, "El celular");

        errores.LanzarSiHayErrores();
    }
}
