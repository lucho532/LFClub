namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa un error previsto de un caso de uso, que la API traduce a
/// <c>application/problem+json</c> (constitución §23).
/// Su responsabilidad es llevar el código estable del contrato, el estado HTTP, el mensaje en
/// español y, cuando son datos no válidos, los errores por campo.
/// No representa errores inesperados ni decide cómo se escribe la respuesta.
/// </summary>
public class ExcepcionDeAplicacion : Exception
{
    /// <summary>Crea el error con su código estable, su estado HTTP y su mensaje.</summary>
    public ExcepcionDeAplicacion(
        string codigo,
        int estadoHttp,
        string mensaje,
        IReadOnlyDictionary<string, string[]>? errores = null)
        : base(mensaje)
    {
        Codigo = codigo;
        EstadoHttp = estadoHttp;
        Errores = errores;
    }

    /// <summary>Código estable definido en el contrato de la API.</summary>
    public string Codigo { get; }

    /// <summary>Estado HTTP con el que responde la API.</summary>
    public int EstadoHttp { get; }

    /// <summary>Mensajes por campo; solo en <c>datos_invalidos</c>.</summary>
    public IReadOnlyDictionary<string, string[]>? Errores { get; }

    /// <summary>Error 404 <c>no_encontrado</c>.</summary>
    public static ExcepcionDeAplicacion NoEncontrado() =>
        new("no_encontrado", 404, "No se encontró lo que buscas.");

    /// <summary>Error 409 con el código indicado.</summary>
    public static ExcepcionDeAplicacion Conflicto(string codigo, string mensaje) => new(codigo, 409, mensaje);
}
