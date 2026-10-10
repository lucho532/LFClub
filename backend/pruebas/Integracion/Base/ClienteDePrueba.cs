using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaPecosa.Pruebas.Integracion.Base;

/// <summary>
/// Cliente HTTP de las pruebas: inicia sesión, envía peticiones con el token y, si se le indica,
/// con el jugador elegido, y lee las respuestas con el mismo formato JSON del contrato.
/// </summary>
public class ClienteDePrueba
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public const string CabeceraJugadorElegido = "X-Jugador-Elegido";

    private readonly FabricaApi _fabrica;

    public ClienteDePrueba(FabricaApi fabrica, HttpClient http)
    {
        _fabrica = fabrica;
        Http = http;
    }

    public HttpClient Http { get; }

    public string? Token { get; private set; }

    /// <summary>Inicia sesión por la API; devuelve la respuesta para poder comprobar los fallos.</summary>
    public async Task<HttpResponseMessage> IniciarSesionAsync(string identificador, string contrasena = Sembrador.Contrasena)
    {
        var respuesta = await PostAsync("/api/sesion", new { identificador, contrasena });
        await respuesta.Content.LoadIntoBufferAsync();
        if (respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await LeerAsync<JsonElement>(respuesta);
            UsarToken(cuerpo.GetProperty("token").GetString()!);
        }

        return respuesta;
    }

    /// <summary>Abre la sesión de una cuenta sin pasar por el inicio de sesión.</summary>
    public async Task<ClienteDePrueba> ConSesionDeAsync(Usuario usuario)
    {
        var actual = await _fabrica.ConContextoAsync(contexto =>
            contexto.Usuarios.AsNoTracking().FirstAsync(cuenta => cuenta.Id == usuario.Id));
        var emisor = _fabrica.Services.GetRequiredService<IEmisorTokenSesion>();
        UsarToken(emisor.Emitir(actual.Id, actual.SelloSeguridad).Token);
        return this;
    }

    /// <summary>
    /// Abre una sesión limitada a esos integrantes, como la que deja entrar con el documento de un
    /// jugador, sin pasar por el inicio de sesión.
    /// </summary>
    public async Task<ClienteDePrueba> ConSesionLimitadaAsync(Usuario usuario, params Guid[] jugadores)
    {
        var actual = await _fabrica.ConContextoAsync(contexto =>
            contexto.Usuarios.AsNoTracking().FirstAsync(cuenta => cuenta.Id == usuario.Id));
        var emisor = _fabrica.Services.GetRequiredService<IEmisorTokenSesion>();
        UsarToken(emisor.Emitir(actual.Id, actual.SelloSeguridad, jugadores).Token);
        return this;
    }

    /// <summary>
    /// Pone en las peticiones siguientes la cabecera <c>X-Jugador-Elegido</c> con ese integrante,
    /// o la quita si es nulo.
    /// </summary>
    public ClienteDePrueba ElegirJugador(Guid? usuarioRolId) => ElegirJugador(usuarioRolId?.ToString());

    /// <summary>Pone la cabecera con un texto cualquiera, para probar los que no son un identificador.</summary>
    public ClienteDePrueba ElegirJugador(string? valor)
    {
        Http.DefaultRequestHeaders.Remove(CabeceraJugadorElegido);
        if (valor is not null)
        {
            Http.DefaultRequestHeaders.TryAddWithoutValidation(CabeceraJugadorElegido, valor);
        }

        return this;
    }

    /// <summary>Abre la sesión de la cuenta DESARROLLADOR.</summary>
    public async Task<ClienteDePrueba> ConSesionDeDesarrolladorAsync()
    {
        var desarrollador = await _fabrica.ConContextoAsync(contexto =>
            contexto.Usuarios.AsNoTracking().FirstAsync(cuenta => cuenta.EsDesarrollador));
        return await ConSesionDeAsync(desarrollador);
    }

    public void UsarToken(string token)
    {
        Token = token;
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void CerrarSesion()
    {
        Token = null;
        Http.DefaultRequestHeaders.Authorization = null;
    }

    public Task<HttpResponseMessage> GetAsync(string ruta) => Http.GetAsync(ruta);

    public Task<HttpResponseMessage> PostAsync(string ruta, object? cuerpo = null) =>
        cuerpo is null ? Http.PostAsync(ruta, null) : Http.PostAsJsonAsync(ruta, cuerpo, Json);

    public Task<HttpResponseMessage> PutAsync(string ruta, object cuerpo) => Http.PutAsJsonAsync(ruta, cuerpo, Json);

    public Task<HttpResponseMessage> DeleteAsync(string ruta) => Http.DeleteAsync(ruta);

    /// <summary>Envía un archivo en el campo <c>archivo</c> de un formulario.</summary>
    public Task<HttpResponseMessage> PutArchivoAsync(string ruta, byte[] contenido, string nombre = "archivo.bin")
    {
        var formulario = new MultipartFormDataContent();
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formulario.Add(archivo, "archivo", nombre);
        return Http.PutAsync(ruta, formulario);
    }

    /// <summary>Lee el cuerpo JSON; se puede llamar varias veces sobre la misma respuesta.</summary>
    public static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta) =>
        JsonSerializer.Deserialize<T>(await respuesta.Content.ReadAsStringAsync(), Json)!;

    /// <summary>Lee el <c>codigo</c> de una respuesta <c>application/problem+json</c>.</summary>
    public static async Task<string?> CodigoAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = await LeerAsync<JsonElement>(respuesta);
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("title").GetString()));
        Assert.Equal((int)respuesta.StatusCode, problema.GetProperty("status").GetInt32());
        return problema.GetProperty("codigo").GetString();
    }

    /// <summary>Lee el <c>title</c> de una respuesta <c>application/problem+json</c>.</summary>
    public static async Task<string> TituloAsync(HttpResponseMessage respuesta) =>
        (await LeerAsync<JsonElement>(respuesta)).GetProperty("title").GetString()!;
}
