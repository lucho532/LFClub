using System.Text.Json;
using System.Text.RegularExpressions;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Carta;

/// <summary>
/// Ninguna lista entrega, ordena ni compara estadísticas, valoraciones o rarezas de carta de más de
/// un jugador (spec 010, escenario 3.8, RF-025, CE-007).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public partial class SinClasificacionPruebas
{
    private static readonly string[] DeCarta = ["valoracion", "estadisticas", "ritmo", "tiro", "pase", "regate", "defensa", "fisico", "rareza"];

    private readonly FabricaApi _fabrica;

    public SinClasificacionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Ningun_esquema_de_lista_de_swagger_lleva_valores_de_carta()
    {
        var texto = await (await _fabrica.CreateClient().GetAsync("/swagger/v1/swagger.json")).Content.ReadAsStringAsync();
        var documento = JsonDocument.Parse(texto).RootElement;
        var esquemas = documento.GetProperty("components").GetProperty("schemas");
        Assert.NotEmpty(Listas(documento));
        Assert.Contains("rareza", Propiedades(esquemas.GetProperty("CartaDto"), esquemas, []));

        foreach (var lista in Listas(documento))
        {
            var encontrados = Propiedades(lista.GetProperty("items"), esquemas, []).Intersect(DeCarta).ToList();
            Assert.True(encontrados.Count == 0, $"Una lista lleva {string.Join(", ", encontrados)}");
        }
    }

    [Fact]
    public void Ningun_esquema_de_lista_de_los_contratos_lleva_valores_de_carta()
    {
        foreach (var contrato in EndpointsDeLaApi.RutasDeLosContratos())
        {
            var lineas = File.ReadAllLines(contrato);
            var conValoresDeCarta = EsquemasConValoresDeCarta(lineas);
            if (contrato.Contains("010-", StringComparison.Ordinal))
            {
                Assert.Contains("CartaDto", conValoresDeCarta);
            }

            foreach (var (linea, numero) in lineas.Select((linea, indice) => (linea, indice + 1)))
            {
                if (linea.Contains("items", StringComparison.Ordinal) && Referencia().Match(linea) is { Success: true } referencia)
                {
                    Assert.False(
                        conValoresDeCarta.Contains(referencia.Groups[1].Value),
                        $"{contrato}:{numero} lista {referencia.Groups[1].Value}");
                }
            }
        }
    }

    [Fact]
    public async Task Las_listas_del_club_no_llevan_nada_de_la_carta()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        foreach (var numero in new[] { 3, 4, 5 })
        {
            await _fabrica.Cartas.PonerCartaAsync(e.Jn(numero).Integrante, numero, PosicionJugador.DEFENSA, [70 + numero, 65, 80, 72, 60, 88]);
        }

        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], _fabrica.Reloj.Hoy.AddDays(-1), convocados: [e.Jn(3).Integrante, e.Jn(4).Integrante]);
        await _fabrica.Actividades.RegistrarJugadoAsync(partido, 2, 0, (e.Jn(3).Integrante, 1), (e.Jn(4).Integrante, 1));

        var rutas = new[]
        {
            e.Ruta("/jugadores"),
            e.Ruta("/goleadores"),
            e.Ruta($"/partidos/{partido.Id}/jugadores"),
            e.Ruta($"/categorias/{e.C2014.Id}"),
        };

        foreach (var ruta in rutas)
        {
            var texto = (await Actividades.LeerAsync(await e.P.Cliente.GetAsync(ruta))).GetRawText();
            foreach (var campo in DeCarta)
            {
                Assert.DoesNotContain($"\"{campo}\"", texto, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // Cada esquema "type: array" del documento, en rutas y componentes.
    private static IEnumerable<JsonElement> Listas(JsonElement elemento)
    {
        if (elemento.ValueKind == JsonValueKind.Object)
        {
            if (elemento.TryGetProperty("type", out var tipo) && tipo.ValueKind == JsonValueKind.String
                && tipo.GetString() == "array" && elemento.TryGetProperty("items", out _))
            {
                yield return elemento;
            }

            foreach (var hijo in elemento.EnumerateObject().SelectMany(propiedad => Listas(propiedad.Value)))
            {
                yield return hijo;
            }
        }
        else if (elemento.ValueKind == JsonValueKind.Array)
        {
            foreach (var hijo in elemento.EnumerateArray().SelectMany(Listas))
            {
                yield return hijo;
            }
        }
    }

    // Los nombres de las propiedades de un esquema, siguiendo sus referencias y sus listas.
    private static IEnumerable<string> Propiedades(JsonElement esquema, JsonElement esquemas, HashSet<string> vistos)
    {
        if (esquema.TryGetProperty("$ref", out var referencia))
        {
            var nombre = referencia.GetString()!.Split('/')[^1];
            return vistos.Add(nombre) ? Propiedades(esquemas.GetProperty(nombre), esquemas, vistos) : [];
        }

        var nombres = new List<string>();
        if (esquema.TryGetProperty("properties", out var propiedades))
        {
            foreach (var propiedad in propiedades.EnumerateObject())
            {
                nombres.Add(propiedad.Name);
                nombres.AddRange(Propiedades(propiedad.Value, esquemas, vistos));
            }
        }

        foreach (var clave in new[] { "items", "allOf", "oneOf", "anyOf" })
        {
            if (esquema.TryGetProperty(clave, out var hijo))
            {
                var hijos = hijo.ValueKind == JsonValueKind.Array ? hijo.EnumerateArray().ToList() : [hijo];
                nombres.AddRange(hijos.SelectMany(parte => Propiedades(parte, esquemas, vistos)));
            }
        }

        return nombres;
    }

    // Los esquemas de components/schemas que tienen una propiedad con un valor de carta.
    private static HashSet<string> EsquemasConValoresDeCarta(string[] lineas)
    {
        var resultado = new HashSet<string>();
        string? actual = null;
        var enEsquemas = false;
        foreach (var linea in lineas)
        {
            enEsquemas |= linea == "  schemas:";
            if (!enEsquemas)
            {
                continue;
            }

            if (Esquema().Match(linea) is { Success: true } esquema)
            {
                actual = esquema.Groups[1].Value;
            }
            else if (actual is not null && DeCarta.Any(campo => linea.TrimStart().StartsWith($"{campo}:", StringComparison.Ordinal)))
            {
                resultado.Add(actual);
            }
        }

        return resultado;
    }

    [GeneratedRegex(@"\$ref: '#/components/schemas/(\w+)'")]
    private static partial Regex Referencia();

    [GeneratedRegex(@"^    (\w+):\s*$")]
    private static partial Regex Esquema();
}
