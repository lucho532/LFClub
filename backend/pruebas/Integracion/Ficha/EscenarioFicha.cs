using System.Net.Http.Headers;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// El club de <see cref="EscenarioCategorias"/> con cuatro jugadores más: Ana, en la categoría
/// 2014, que entrena el entrenador del club; Beto, en la 2015, sin entrenador; Caro, sin categoría;
/// y Dani, retirado. Cada uno con su cliente con sesión. Reúne las rutas de la ficha y la lectura
/// directa de lo guardado.
/// </summary>
internal sealed class EscenarioFicha
{
    public const string CelularNuevo = "3209998877";
    public const string ResponsableNuevo = "Marta Gómez";

    private EscenarioFicha()
    {
    }

    public required EscenarioCategorias Base { get; init; }

    public required Categoria CategoriaDeAna { get; init; }

    public required Categoria CategoriaDeBeto { get; init; }

    public required UsuarioRol Ana { get; init; }

    public required ClienteDePrueba CuentaDeAna { get; init; }

    public required UsuarioRol Beto { get; init; }

    public required ClienteDePrueba CuentaDeBeto { get; init; }

    public required UsuarioRol Caro { get; init; }

    public required ClienteDePrueba CuentaDeCaro { get; init; }

    public required UsuarioRol Dani { get; init; }

    public required ClienteDePrueba CuentaDeDani { get; init; }

    public FabricaApi Fabrica => Base.Fabrica;

    public Club Club => Base.Club;

    public ClienteDePrueba Presidente => Base.Presidente;

    public ClienteDePrueba Directivo => Base.Directivo;

    public ClienteDePrueba Entrenador => Base.Entrenador;

    /// <summary>Los dos roles del club que ven fichas y nunca cambian nada.</summary>
    public IEnumerable<(string Rol, ClienteDePrueba Cliente)> QuienesSoloConsultan =>
        [("DIRECTIVO", Directivo), ("ENTRENADOR", Entrenador)];

    public static async Task<EscenarioFicha> CrearAsync(FabricaApi fabrica)
    {
        var escenario = await EscenarioCategorias.CrearAsync(fabrica);
        var sembrar = fabrica.Categorias;
        var categoria2014 = await sembrar.CrearCategoriaAsync(escenario.Club, 2014);
        var categoria2015 = await sembrar.CrearCategoriaAsync(escenario.Club, 2015);
        await sembrar.AsignarEntrenadorAsync(categoria2014, escenario.IntegranteEntrenador);

        var (cuentaAna, ana) = await sembrar.CrearJugadorAsync(escenario.Club, 2014, categoria2014, "Ana");
        var (cuentaBeto, beto) = await sembrar.CrearJugadorAsync(escenario.Club, 2015, categoria2015, "Beto");
        var (cuentaCaro, caro) = await sembrar.CrearJugadorAsync(escenario.Club, 2016, nombres: "Caro");
        var (cuentaDani, dani) = await sembrar.CrearJugadorAsync(escenario.Club, 2014, categoria2014, "Dani");
        await sembrar.RetirarAsync(dani, escenario.IntegrantePresidente);

        return new EscenarioFicha
        {
            Base = escenario,
            CategoriaDeAna = categoria2014,
            CategoriaDeBeto = categoria2015,
            Ana = ana,
            CuentaDeAna = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaAna),
            Beto = beto,
            CuentaDeBeto = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaBeto),
            Caro = caro,
            CuentaDeCaro = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaCaro),
            Dani = dani,
            CuentaDeDani = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaDani),
        };
    }

    public static string Ficha(Guid clubId, Guid usuarioRolId) => $"/api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha";

    public string Ficha(Guid usuarioRolId) => Ficha(Club.Id, usuarioRolId);

    public string Identidad(Guid usuarioRolId) => $"{Ficha(usuarioRolId)}/identidad";

    public string DocumentoIdentidad(Guid usuarioRolId) => $"{Ficha(usuarioRolId)}/documento-identidad";

    public string Documento(Guid usuarioRolId, DocumentoPedido documento) =>
        $"{Ficha(usuarioRolId)}/documentos/{documento}";

    /// <summary>
    /// Cuerpo de <c>PUT …/ficha</c> con los doce datos; cada prueba cambia o añade los que necesita.
    /// </summary>
    public static Dictionary<string, object?> Cuerpo(
        string? celular = CelularNuevo, string? nombreResponsable = ResponsableNuevo) => new()
        {
            ["celular"] = celular,
            ["nombreResponsable"] = nombreResponsable,
            ["emergenciaNombre"] = "Luis Gómez",
            ["emergenciaParentesco"] = "Padre",
            ["emergenciaCelular"] = "3151112233",
            ["entidadSalud"] = "Nueva EPS",
            ["lugarAtencion"] = "Hospital de Caldas",
            ["grupoSanguineo"] = "A_POSITIVO",
            ["alergias"] = "Polen",
            ["enfermedades"] = "Ninguna conocida",
            ["medicamentos"] = "Loratadina",
            ["observaciones"] = "Usa gafas",
        };

    /// <summary>La ficha tal como la recibe ese cliente; falla si no responde 200.</summary>
    public async Task<JsonElement> FichaDeAsync(ClienteDePrueba cliente, Guid usuarioRolId)
    {
        var respuesta = await cliente.GetAsync(Ficha(usuarioRolId));
        Assert.True(respuesta.IsSuccessStatusCode, $"GET ficha: {(int)respuesta.StatusCode}");
        return await respuesta.JsonAsync();
    }

    /// <summary>La fila de la ficha tal como está guardada, o nulo si nadie la ha cambiado.</summary>
    public Task<FichaJugador?> FichaGuardadaAsync(Guid usuarioRolId) => Fabrica.ConContextoAsync(contexto =>
        contexto.FichasJugador.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(ficha => ficha.UsuarioRolId == usuarioRolId));

    /// <summary>Los archivos guardados de ese jugador.</summary>
    public Task<List<DocumentoJugador>> DocumentosGuardadosAsync(Guid usuarioRolId) => Fabrica.ConContextoAsync(contexto =>
        contexto.DocumentosJugador.IgnoreQueryFilters().AsNoTracking()
            .Where(archivo => archivo.UsuarioRolId == usuarioRolId)
            .ToListAsync());

    /// <summary>El jugador tal como está guardado, con su cuenta y sus equipos.</summary>
    public Task<UsuarioRol> JugadorGuardadoAsync(Guid usuarioRolId) => Fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking()
            .Include(integrante => integrante.Usuario)
            .Include(integrante => integrante.Equipos)
            .FirstAsync(integrante => integrante.Id == usuarioRolId));
}

/// <summary>Envíos y lecturas que repiten las pruebas de la ficha.</summary>
internal static class AyudasDeFicha
{
    /// <summary>Envía el archivo en el campo <c>archivo</c> de un formulario <c>multipart/form-data</c>.</summary>
    public static Task<HttpResponseMessage> SubirAsync(
        this ClienteDePrueba cliente, string ruta, byte[] contenido, string nombre = "archivo.pdf") =>
        cliente.PutArchivoAsync(ruta, contenido, nombre);

    /// <summary>Envía un formulario sin el campo <c>archivo</c>.</summary>
    public static Task<HttpResponseMessage> SubirSinArchivoAsync(this ClienteDePrueba cliente, string ruta)
    {
        var formulario = new MultipartFormDataContent { { new StringContent("nada"), "otro" } };
        return cliente.Http.PutAsync(ruta, formulario);
    }

    /// <summary>Indica si el objeto JSON trae esa propiedad, aunque sea nula.</summary>
    public static bool Tiene(this JsonElement elemento, string propiedad) => elemento.TryGetProperty(propiedad, out _);

    /// <summary>El documento pedido dentro de <c>documentos</c>.</summary>
    public static JsonElement DocumentoDe(this JsonElement ficha, DocumentoPedido documento) =>
        ficha.GetProperty("documentos").EnumerateArray()
            .Single(elemento => elemento.GetProperty("documento").GetString() == documento.ToString());

    /// <summary>El tipo de contenido de una respuesta, sin parámetros.</summary>
    public static string? Tipo(this HttpResponseMessage respuesta) => respuesta.Content.Headers.ContentType?.MediaType;

    /// <summary>El valor de <c>Content-Disposition</c> tal como llegó.</summary>
    public static string? Disposicion(this HttpResponseMessage respuesta) =>
        respuesta.Content.Headers.TryGetValues("Content-Disposition", out var valores) ? valores.Single() : null;

    /// <summary>Indica si la respuesta prohíbe guardarla en caché.</summary>
    public static bool SinCache(this HttpResponseMessage respuesta) =>
        respuesta.Headers.CacheControl is CacheControlHeaderValue { Private: true, NoStore: true };
}
