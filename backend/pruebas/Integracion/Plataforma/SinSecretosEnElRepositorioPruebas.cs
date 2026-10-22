using System.Text.Json;
using System.Text.RegularExpressions;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>
/// Ninguna llave de la pasarela está escrita en el repositorio (constitución §7.4, spec 017, RF-025 y
/// CE-008). Recorre los archivos de código, de configuración y de documentación buscando los prefijos
/// con los que Wompi entrega sus llaves, y comprueba que la sección <c>Wompi</c> de la configuración
/// versionada esté vacía.
/// </summary>
public partial class SinSecretosEnElRepositorioPruebas
{
    private static readonly string[] CarpetasIgnoradas = ["bin", "obj", "node_modules", ".git", "dist", "android", ".vs"];

    private static readonly string[] Extensiones =
        [".cs", ".json", ".md", ".yaml", ".yml", ".ts", ".tsx", ".js", ".css", ".html", ".xml", ".csproj", ".env", ".ps1", ".sh"];

    private static string RaizDelRepositorio()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !Directory.Exists(Path.Combine(carpeta.FullName, "specs")))
        {
            carpeta = carpeta.Parent;
        }

        return carpeta?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }

    private static IEnumerable<string> Archivos(string carpeta) =>
        Directory.EnumerateFiles(carpeta).Where(archivo => Extensiones.Contains(Path.GetExtension(archivo).ToLowerInvariant()))
            .Concat(Directory.EnumerateDirectories(carpeta)
                .Where(sub => !CarpetasIgnoradas.Contains(Path.GetFileName(sub)))
                .SelectMany(Archivos));

    [Fact]
    public void Ningun_archivo_versionado_contiene_una_llave_de_la_pasarela()
    {
        var hallazgos = new List<string>();
        foreach (var archivo in Archivos(RaizDelRepositorio()))
        {
            var texto = File.ReadAllText(archivo);
            if (PrefijoDeLlave().IsMatch(texto))
            {
                hallazgos.Add(Path.GetRelativePath(RaizDelRepositorio(), archivo));
            }
        }

        Assert.Empty(hallazgos);
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void La_configuracion_versionada_no_trae_valores_de_la_pasarela(string nombre)
    {
        var ruta = Path.Combine(RaizDelRepositorio(), "backend", "src", "LFClub.Api", nombre);
        using var documento = JsonDocument.Parse(File.ReadAllText(ruta));

        if (!documento.RootElement.TryGetProperty("Wompi", out var wompi))
        {
            return;
        }

        foreach (var propiedad in wompi.EnumerateObject())
        {
            if (propiedad.Name == "UrlCheckout")
            {
                continue;
            }

            Assert.True(
                propiedad.Value.ValueKind == JsonValueKind.String && propiedad.Value.GetString() == string.Empty,
                $"{nombre}: Wompi:{propiedad.Name} debe estar vacío.");
        }
    }

    [GeneratedRegex(@"(pub_(prod|test)_|(prod|test)_integrity_|(prod|test)_events_)[A-Za-z0-9]{6,}")]
    private static partial Regex PrefijoDeLlave();
}
