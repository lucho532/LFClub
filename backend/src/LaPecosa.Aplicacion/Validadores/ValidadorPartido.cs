using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de lo que se envía al crear o cambiar un partido (spec 009, RF-007,
/// escenario 2.10).
/// Su responsabilidad es exigir rival y lugar obligatorios de 120 caracteres como máximo, fecha y
/// hora con formato, condición y tipo, el nombre del torneo obligatorio si es de torneo (y
/// descartarlo si es amistoso) y una o más categorías sin repetir, y devolverlo ya convertido. Si
/// algo falla lanza <c>datos_invalidos</c> con el error en cada campo.
/// No comprueba que las categorías existan, estén activas o las entrene quien pregunta.
/// </summary>
public static class ValidadorPartido
{
    /// <summary>Valida los datos y los devuelve convertidos.</summary>
    public static DatosDePartido Validar(GuardarPartidoDto datos)
    {
        var errores = new ErroresDeValidacion();
        var rival = CamposDeActividad.Limpio(datos.Rival);
        errores.Obligatorio("rival", rival, Partido.LongitudMaximaDeTexto, "El rival");
        var fecha = CamposDeActividad.Fecha(errores, "fecha", datos.Fecha, "La fecha");
        var hora = CamposDeActividad.Hora(errores, "hora", datos.Hora, "La hora");
        var lugar = CamposDeActividad.Limpio(datos.Lugar);
        errores.Obligatorio("lugar", lugar, Partido.LongitudMaximaDeTexto, "El lugar");
        if (datos.Condicion is null)
        {
            errores.Agregar("condicion", "Indica si el partido es de local o de visitante.");
        }

        if (datos.Tipo is null)
        {
            errores.Agregar("tipo", "Indica si el partido es amistoso o de torneo.");
        }

        // Un amistoso no lleva torneo: lo que llegue se descarta.
        var torneo = datos.Tipo == TipoPartido.TORNEO ? CamposDeActividad.Limpio(datos.NombreTorneo) : null;
        if (datos.Tipo == TipoPartido.TORNEO)
        {
            errores.Obligatorio("nombreTorneo", torneo, Partido.LongitudMaximaDeTexto, "El nombre del torneo");
        }

        var categorias = CamposDeActividad.Categorias(errores, datos.CategoriaIds);
        errores.LanzarSiHayErrores();

        return new DatosDePartido(
            rival!, fecha!.Value, hora!.Value, lugar!, datos.Condicion!.Value, datos.Tipo!.Value, torneo, categorias);
    }
}

/// <summary>
/// Representa los datos ya validados de un partido.
/// Su responsabilidad es llevarlos convertidos y limpios.
/// No indica si las categorías se pueden usar.
/// </summary>
/// <param name="Rival">Equipo rival.</param>
/// <param name="Fecha">Fecha, en hora de Colombia.</param>
/// <param name="Hora">Hora, en hora de Colombia.</param>
/// <param name="Lugar">Lugar.</param>
/// <param name="Condicion">Local o visitante.</param>
/// <param name="Tipo">Amistoso o de torneo.</param>
/// <param name="NombreTorneo">Nombre del torneo; nulo en un amistoso.</param>
/// <param name="CategoriaIds">Categorías, sin repetir.</param>
public record DatosDePartido(
    string Rival,
    DateOnly Fecha,
    TimeOnly Hora,
    string Lugar,
    CondicionPartido Condicion,
    TipoPartido Tipo,
    string? NombreTorneo,
    IReadOnlyList<Guid> CategoriaIds);
