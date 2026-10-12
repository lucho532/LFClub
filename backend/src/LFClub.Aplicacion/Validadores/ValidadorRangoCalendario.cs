using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación del rango que se pide al calendario (spec 009, research §12).
/// Su responsabilidad es exigir las dos fechas con formato, que la primera no sea posterior a la
/// segunda y que el rango no pase de 62 días, ambos incluidos; si no, lanza <c>datos_invalidos</c>.
/// No decide qué actividades entran en el rango.
/// </summary>
public static class ValidadorRangoCalendario
{
    /// <summary>Máximo de días de un rango, contando los dos extremos.</summary>
    public const int MaximoDeDias = 62;

    /// <summary>Valida el rango y lo devuelve convertido.</summary>
    public static (DateOnly Desde, DateOnly Hasta) Validar(string? desde, string? hasta)
    {
        var errores = new ErroresDeValidacion();
        var inicio = CamposDeActividad.Fecha(errores, "desde", desde, "La fecha inicial");
        var fin = CamposDeActividad.Fecha(errores, "hasta", hasta, "La fecha final");
        if (inicio is not null && fin is not null)
        {
            if (fin < inicio)
            {
                errores.Agregar("hasta", "La fecha final no puede ser anterior a la inicial.");
            }
            else if (fin.Value.DayNumber - inicio.Value.DayNumber + 1 > MaximoDeDias)
            {
                errores.Agregar("hasta", $"El calendario muestra como máximo {MaximoDeDias} días.");
            }
        }

        errores.LanzarSiHayErrores();
        return (inicio!.Value, fin!.Value);
    }
}
