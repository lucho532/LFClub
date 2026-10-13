using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Errores;

/// <summary>
/// Representa el cuerpo <c>application/problem+json</c> de todos los errores de la API
/// (constitución §23 y esquema <c>Problema</c> del contrato).
/// Su responsabilidad es llevar el mensaje en español, el estado HTTP, el código estable y, en
/// <c>datos_invalidos</c>, los errores por campo.
/// No contiene detalles internos del servidor.
/// </summary>
public class Problema
{
    /// <summary>Tipo de contenido de los errores.</summary>
    public const string TipoContenido = "application/problem+json";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Crea el problema con su estado, su código y su mensaje.</summary>
    public Problema(
        int status,
        string codigo,
        string title,
        IReadOnlyDictionary<string, string[]>? errores = null,
        IReadOnlyDictionary<string, object>? extras = null)
    {
        Status = status;
        Codigo = codigo;
        Title = title;
        Errores = errores;
        Extras = extras?.ToDictionary(par => par.Key, par => par.Value);
    }

    /// <summary>
    /// Campos propios de un código concreto, escritos junto a los demás (por ejemplo <c>clubes</c>
    /// en <c>presidente_unico</c>).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Extras { get; set; }

    /// <summary>Mensaje en español para mostrar a la persona.</summary>
    public string Title { get; }

    /// <summary>Estado HTTP.</summary>
    public int Status { get; }

    /// <summary>Código estable para el frontend.</summary>
    public string Codigo { get; }

    /// <summary>Mensajes por campo; solo en <c>datos_invalidos</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errores { get; }

    /// <summary>Problema 401 <c>sin_sesion</c>.</summary>
    public static Problema SinSesion() =>
        new(401, "sin_sesion", "Tu sesión no es válida o terminó. Inicia sesión de nuevo.");

    /// <summary>Problema 404 <c>no_encontrado</c>.</summary>
    public static Problema NoEncontrado() => new(404, "no_encontrado", "No se encontró lo que buscas.");

    /// <summary>
    /// Convierte el problema en un resultado de MVC que se escribe siempre como
    /// <c>application/problem+json</c>, sin pasar por la negociación de contenido del controlador.
    /// </summary>
    public IActionResult ComoResultado() => new ResultadoProblema(this);

    /// <summary>Escribe el problema directamente en la respuesta, fuera de MVC.</summary>
    public async Task EscribirAsync(HttpContext contexto)
    {
        if (contexto.Response.HasStarted)
        {
            return;
        }

        contexto.Response.StatusCode = Status;
        contexto.Response.ContentType = TipoContenido;
        await JsonSerializer.SerializeAsync(contexto.Response.Body, this, OpcionesJson, contexto.RequestAborted);
    }

    /// <summary>Resultado de MVC que escribe el problema tal cual.</summary>
    private sealed class ResultadoProblema(Problema problema) : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context) => problema.EscribirAsync(context.HttpContext);
    }
}
