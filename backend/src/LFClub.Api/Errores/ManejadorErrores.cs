using LFClub.Aplicacion.Utilidades;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Errores;

/// <summary>
/// Representa la traducción de los errores de la API a <c>application/problem+json</c>
/// (constitución §23).
/// Su responsabilidad es convertir los errores previstos de los casos de uso, los datos de entrada
/// no válidos y cualquier error inesperado en una respuesta con título en español, estado y código.
/// No contiene reglas de negocio y nunca devuelve detalles internos: un error inesperado se
/// registra y responde un 500 genérico.
/// </summary>
public class ManejadorErrores : IExceptionHandler
{
    private readonly ILogger<ManejadorErrores> _registro;

    /// <summary>Crea el manejador con su registro.</summary>
    public ManejadorErrores(ILogger<ManejadorErrores> registro)
    {
        _registro = registro;
    }

    /// <summary>Respuesta 400 <c>datos_invalidos</c> para un cuerpo que no se pudo leer.</summary>
    public static IActionResult DatosInvalidos(ActionContext contexto)
    {
        var errores = contexto.ModelState
            .Where(par => par.Value is { Errors.Count: > 0 })
            .ToDictionary(
                par => NombreDeCampo(par.Key),
                _ => new[] { "El valor no es válido." });

        return new Problema(400, "datos_invalidos", "Revisa los datos marcados.", errores).ComoResultado();
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problema = exception switch
        {
            ExcepcionDeAplicacion prevista =>
                new Problema(prevista.EstadoHttp, prevista.Codigo, prevista.Message, prevista.Errores, prevista.Extras),
            BadHttpRequestException peticion when peticion.StatusCode == StatusCodes.Status413PayloadTooLarge =>
                new Problema(413, "peticion_demasiado_grande", "Lo que enviaste es demasiado grande."),
            BadHttpRequestException =>
                new Problema(400, "datos_invalidos", "No se pudo leer lo que enviaste."),
            _ => null,
        };

        if (problema is null)
        {
            _registro.LogError(exception, "Error inesperado en {Ruta}", httpContext.Request.Path);
            problema = new Problema(500, "error_interno", "Ocurrió un error inesperado. Inténtalo de nuevo.");
        }

        await problema.EscribirAsync(httpContext);
        return true;
    }

    private static string NombreDeCampo(string clave)
    {
        var nombre = clave.StartsWith("$.", StringComparison.Ordinal) ? clave[2..] : clave;
        return nombre.Length == 0 || nombre == "$" ? "cuerpo" : char.ToLowerInvariant(nombre[0]) + nombre[1..];
    }
}
