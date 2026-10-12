using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de lo que se envía al programar o cambiar un entrenamiento (spec 009,
/// RF-005).
/// Su responsabilidad es exigir fecha y horas con formato, una hora de fin posterior a la de inicio,
/// el lugar obligatorio de 120 caracteres como máximo y sin espacios a los lados, la nota de 500
/// como máximo y una o más categorías sin repetir, y devolverlo ya convertido. Si algo falla lanza
/// <c>datos_invalidos</c> con el error en cada campo.
/// No comprueba que las categorías existan, estén activas o las entrene quien pregunta, y admite
/// fechas pasadas (research §16.4).
/// </summary>
public static class ValidadorEntrenamiento
{
    /// <summary>Valida los datos y los devuelve convertidos.</summary>
    public static DatosDeEntrenamiento Validar(GuardarEntrenamientoDto datos)
    {
        var errores = new ErroresDeValidacion();
        var fecha = CamposDeActividad.Fecha(errores, "fecha", datos.Fecha, "La fecha");
        var inicio = CamposDeActividad.Hora(errores, "horaInicio", datos.HoraInicio, "La hora de inicio");
        var fin = CamposDeActividad.Hora(errores, "horaFin", datos.HoraFin, "La hora de fin");
        if (inicio is not null && fin is not null && fin <= inicio)
        {
            errores.Agregar("horaFin", "La hora de fin debe ser posterior a la de inicio.");
        }

        var lugar = CamposDeActividad.Limpio(datos.Lugar);
        errores.Obligatorio("lugar", lugar, Entrenamiento.LongitudMaximaDelLugar, "El lugar");
        var nota = CamposDeActividad.Limpio(datos.Nota);
        errores.Maximo("nota", nota, Entrenamiento.LongitudMaximaDeLaNota, "La nota");
        var categorias = CamposDeActividad.Categorias(errores, datos.CategoriaIds);
        errores.LanzarSiHayErrores();

        return new DatosDeEntrenamiento(fecha!.Value, inicio!.Value, fin!.Value, lugar!, nota, categorias);
    }
}

/// <summary>
/// Representa los datos ya validados de un entrenamiento.
/// Su responsabilidad es llevarlos convertidos a fecha, horas y textos limpios.
/// No indica si las categorías se pueden usar.
/// </summary>
/// <param name="Fecha">Fecha, en hora de Colombia.</param>
/// <param name="HoraInicio">Hora de inicio.</param>
/// <param name="HoraFin">Hora de fin.</param>
/// <param name="Lugar">Lugar, sin espacios a los lados.</param>
/// <param name="Nota">Nota, o nula si no tiene.</param>
/// <param name="CategoriaIds">Categorías, sin repetir.</param>
public record DatosDeEntrenamiento(
    DateOnly Fecha,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    string Lugar,
    string? Nota,
    IReadOnlyList<Guid> CategoriaIds);
