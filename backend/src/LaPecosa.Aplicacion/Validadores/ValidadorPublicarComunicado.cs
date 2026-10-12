using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de lo que se envía al publicar un comunicado (spec 011, RF-001, RF-002 y
/// RF-010).
/// Su responsabilidad es exigir una clave de publicación, un título de 100 caracteres como máximo y
/// un texto de 1.000 como máximo, ambos obligatorios y sin espacios a los lados, y un alcance
/// conocido, y devolverlo ya convertido, con las categorías y los equipos sin repetir. Si algo falla
/// lanza <c>datos_invalidos</c> con el error en cada campo.
/// No comprueba que la audiencia sea coherente, que las categorías o los equipos existan o estén
/// activos, ni que quien publica pueda dirigirse a ellos: eso es de la regla de audiencia y del
/// servicio.
/// </summary>
public static class ValidadorPublicarComunicado
{
    /// <summary>Valida los datos y los devuelve convertidos.</summary>
    public static DatosDeComunicado Validar(PublicarComunicadoDto datos)
    {
        var errores = new ErroresDeValidacion();
        if (datos.Clave == Guid.Empty)
        {
            errores.Agregar("clave", "La clave de publicación es obligatoria.");
        }

        var titulo = datos.Titulo?.Trim();
        errores.Obligatorio("titulo", titulo, Comunicado.LongitudMaximaDelTitulo, "El título");
        var texto = datos.Texto?.Trim();
        errores.Obligatorio("texto", texto, Comunicado.LongitudMaximaDelTexto, "El texto");
        if (!Enum.IsDefined(datos.Alcance))
        {
            errores.Agregar("alcance", "La audiencia no es válida.");
        }

        errores.LanzarSiHayErrores();

        return new DatosDeComunicado(
            datos.Clave,
            titulo!,
            texto!,
            datos.Alcance,
            datos.CategoriaIds?.Distinct().ToList() ?? [],
            datos.EquipoIds?.Distinct().ToList() ?? []);
    }
}

/// <summary>
/// Representa los datos ya validados de un comunicado.
/// Su responsabilidad es llevarlos convertidos, con los textos limpios y las listas sin repetir.
/// No indica si la audiencia es coherente ni si se puede usar.
/// </summary>
/// <param name="Clave">Clave de publicación.</param>
/// <param name="Titulo">Título, sin espacios a los lados.</param>
/// <param name="Texto">Texto, sin espacios a los lados.</param>
/// <param name="Alcance">Alcance elegido.</param>
/// <param name="CategoriaIds">Categorías, sin repetir; vacía si no se enviaron.</param>
/// <param name="EquipoIds">Equipos, sin repetir; vacía si no se enviaron.</param>
public record DatosDeComunicado(
    Guid Clave,
    string Titulo,
    string Texto,
    AlcanceComunicado Alcance,
    IReadOnlyList<Guid> CategoriaIds,
    IReadOnlyList<Guid> EquipoIds);
