using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace LaPecosa.Pruebas.Integracion.Base;

/// <summary>Un endpoint: su método y su ruta con los parámetros entre llaves.</summary>
public record Endpoint(string Metodo, string Ruta)
{
    /// <summary>La ruta con cada parámetro sustituido por un identificador.</summary>
    public string Con(Guid? clubId = null) => Regex.Replace(
        Ruta, @"\{(\w+)\}", parametro => (parametro.Groups[1].Value == "clubId" ? clubId ?? Guid.NewGuid() : Guid.NewGuid()).ToString());

    public override string ToString() => $"{Metodo} {Ruta}";
}

/// <summary>
/// Lista los endpoints de la propia API y los del contrato, para que las pruebas de acceso cubran
/// también los endpoints que se añadan después sin tener que acordarse de ellos.
/// </summary>
public static partial class EndpointsDeLaApi
{
    /// <summary>Todos los endpoints que expone la API en ejecución.</summary>
    public static List<Endpoint> DeLaApi(FabricaApi fabrica) => fabrica.Services
        .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
        .ApiDescriptionGroups.Items
        .SelectMany(grupo => grupo.Items)
        .Select(descripcion => new Endpoint(
            descripcion.HttpMethod!.ToUpperInvariant(),
            "/" + SinRestricciones().Replace(descripcion.RelativePath!, "{$1}").Split('?')[0]))
        .Distinct()
        .OrderBy(endpoint => endpoint.Ruta).ThenBy(endpoint => endpoint.Metodo)
        .ToList();

    /// <summary>
    /// Los endpoints de los contratos de todas las specs (<c>specs/*/contracts/api.yaml</c>), sin
    /// repetir los que una spec posterior vuelve a describir, con la indicación de si algún
    /// contrato los marca como anónimos (<c>security: []</c>).
    /// </summary>
    public static List<(Endpoint Endpoint, bool Anonimo)> DelContrato() => RutasDeLosContratos()
        .SelectMany(DeUnContrato)
        .GroupBy(par => par.Endpoint)
        .Select(grupo => (grupo.Key, grupo.Any(par => par.Anonimo)))
        .ToList();

    private static List<(Endpoint Endpoint, bool Anonimo)> DeUnContrato(string rutaDelContrato)
    {
        var resultado = new List<(Endpoint, bool)>();
        string? ruta = null;
        string? metodo = null;
        var anonimo = false;

        void Cerrar()
        {
            if (ruta is not null && metodo is not null)
            {
                resultado.Add((new Endpoint(metodo, ruta), anonimo));
            }

            metodo = null;
            anonimo = false;
        }

        foreach (var linea in File.ReadLines(rutaDelContrato))
        {
            if (linea.StartsWith("components:", StringComparison.Ordinal))
            {
                break;
            }

            if (RutaYaml().Match(linea) is { Success: true } nuevaRuta)
            {
                Cerrar();
                ruta = nuevaRuta.Groups[1].Value;
            }
            else if (MetodoYaml().Match(linea) is { Success: true } nuevoMetodo)
            {
                Cerrar();
                metodo = nuevoMetodo.Groups[1].Value.ToUpperInvariant();
            }
            else if (metodo is not null && linea.TrimEnd() == "      security: []")
            {
                anonimo = true;
            }
        }

        Cerrar();
        return resultado;
    }

    /// <summary>Las rutas de los <c>api.yaml</c> de todas las specs, en orden.</summary>
    public static List<string> RutasDeLosContratos()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !Directory.Exists(Path.Combine(carpeta.FullName, "specs")))
        {
            carpeta = carpeta.Parent;
        }

        var specs = Path.Combine(
            carpeta?.FullName ?? throw new InvalidOperationException("No se encontró la carpeta specs."), "specs");

        return Directory.EnumerateDirectories(specs)
            .Select(spec => Path.Combine(spec, "contracts", "api.yaml"))
            .Where(File.Exists)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    [GeneratedRegex(@"\{(\w+)(:\w+)?\}")]
    private static partial Regex SinRestricciones();

    [GeneratedRegex(@"^  (/api/\S+):\s*$")]
    private static partial Regex RutaYaml();

    [GeneratedRegex(@"^    (get|post|put|delete|patch):\s*$")]
    private static partial Regex MetodoYaml();
}
