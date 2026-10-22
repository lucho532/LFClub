using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de un medio de pago del club (spec 015, RF-027).
/// Su responsabilidad es exigir la entidad (hasta 40 caracteres), el titular (hasta 100) y el número
/// (hasta 60), y aceptar una nota opcional de hasta 200, sin espacios sobrantes. Si algo falla lanza
/// <c>datos_invalidos</c> con el error en cada campo.
/// No comprueba que la cuenta exista ni que el número tenga un formato concreto: cada entidad usa el suyo.
/// </summary>
public static class ValidadorMedioPago
{
    /// <summary>Longitud máxima de la entidad.</summary>
    public const int LongitudDeEntidad = 40;

    /// <summary>Longitud máxima del titular.</summary>
    public const int LongitudDelTitular = 100;

    /// <summary>Longitud máxima del número.</summary>
    public const int LongitudDelNumero = 60;

    /// <summary>Longitud máxima de la nota.</summary>
    public const int LongitudDeLaNota = 200;

    /// <summary>Valida los datos y los devuelve limpios.</summary>
    public static (string Entidad, string Titular, string Numero, string? Nota) Validar(GuardarMedioPagoDto datos)
    {
        var errores = new ErroresDeValidacion();
        var entidad = NormalizadorTexto.SinEspaciosSobrantes(datos.Entidad);
        var titular = NormalizadorTexto.SinEspaciosSobrantes(datos.Titular);
        var numero = NormalizadorTexto.SinEspaciosSobrantes(datos.Numero);
        var nota = NormalizadorTexto.SinEspaciosSobrantes(datos.Nota);

        errores.Obligatorio("entidad", entidad, LongitudDeEntidad, "La entidad");
        errores.Obligatorio("titular", titular, LongitudDelTitular, "El titular");
        errores.Obligatorio("numero", numero, LongitudDelNumero, "El número");
        errores.Maximo("nota", nota, LongitudDeLaNota, "La nota");
        errores.LanzarSiHayErrores();

        return (entidad, titular, numero, nota.Length == 0 ? null : nota);
    }
}
