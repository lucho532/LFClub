using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de los datos con los que se agrega un hermano (RF-002, RF-004 y RF-006
/// de la 006).
/// Su responsabilidad es exigir nombres, apellidos, tipo y número de documento y una fecha de
/// nacimiento no futura, con <see cref="ValidadorIdentidad"/>: las mismas reglas y los mismos
/// mensajes del registro y de la ficha. Y, si el hermano es menor de 18 años y la cuenta todavía
/// no tiene responsable, exigir su nombre, con el mensaje del registro.
/// No comprueba si el documento ya existe ni quién agrega al hermano. Si la cuenta ya tiene
/// responsable, o el hermano es adulto, el nombre del responsable ni se exige ni se valida: no se
/// guarda.
/// </summary>
public static class ValidadorHermano
{
    /// <summary>
    /// Lanza <c>datos_invalidos</c> si los datos no son válidos.
    /// <paramref name="cuentaTieneResponsable"/> dice si la cuenta de la familia ya lo tiene.
    /// <paramref name="hoy"/> es la fecha UTC del servidor: con ella se rechazan las fechas futuras
    /// y se decide quién es menor.
    /// </summary>
    public static void Validar(AgregarHermanoDto datos, bool cuentaTieneResponsable, DateOnly hoy)
    {
        var errores = new ErroresDeValidacion();

        ValidadorIdentidad.NombresYApellidos(errores, datos.Nombres, datos.Apellidos);
        ValidadorIdentidad.Documento(errores, datos.TipoDocumento, datos.NumeroDocumento);

        if (ValidadorIdentidad.FechaNacimiento(errores, datos.FechaNacimiento, hoy)
            && GuardaResponsable(datos.FechaNacimiento!.Value, cuentaTieneResponsable, hoy))
        {
            if (string.IsNullOrWhiteSpace(datos.NombreResponsable))
            {
                errores.Agregar(
                    "nombreResponsable",
                    "El nombre del padre, madre o responsable es obligatorio para una persona menor de 18 años.");
            }
            else
            {
                errores.Maximo(
                    "nombreResponsable",
                    datos.NombreResponsable,
                    ValidadorRegistro.MaximoResponsable,
                    "El nombre del responsable");
            }
        }

        errores.LanzarSiHayErrores();
    }

    /// <summary>
    /// Indica si al agregar a ese hermano se guarda en la cuenta el responsable que se escriba:
    /// solo cuando es menor de edad y la cuenta todavía no tiene uno.
    /// </summary>
    public static bool GuardaResponsable(DateOnly fechaNacimiento, bool cuentaTieneResponsable, DateOnly hoy) =>
        !cuentaTieneResponsable && ReglaMayoriaDeEdad.EsMenorDeEdad(fechaNacimiento, hoy);
}
