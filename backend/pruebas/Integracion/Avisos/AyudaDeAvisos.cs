using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// Lecturas de la campana que comparten las pruebas de avisos: los avisos que ve una cuenta, su
/// texto y su contador, tal como los devuelve la API (spec 012).
/// </summary>
public static class AyudaDeAvisos
{
    /// <summary>Los avisos (no los comunicados) que ve la cuenta, del más reciente al más antiguo.</summary>
    public static async Task<List<JsonElement>> AvisosAsync(EscenarioActividades e, Persona quien)
    {
        var pagina = await Actividades.LeerAsync(await quien.Cliente.GetAsync(e.Ruta("/campana?limite=50")));
        return pagina.GetProperty("elementos").EnumerateArray()
            .Where(elemento => elemento.GetProperty("tipo").GetString() == "AVISO")
            .Select(elemento => elemento.GetProperty("aviso").Clone())
            .ToList();
    }

    /// <summary>Los textos de los avisos que ve la cuenta, del más reciente al más antiguo.</summary>
    public static async Task<List<string>> TextosAsync(EscenarioActividades e, Persona quien) =>
        (await AvisosAsync(e, quien)).Select(aviso => aviso.GetProperty("texto").GetString()!).ToList();

    /// <summary>El contador de sin leer de la cuenta (comunicados más avisos).</summary>
    public static async Task<int> SinLeerAsync(EscenarioActividades e, Persona quien) =>
        (await Actividades.LeerAsync(await quien.Cliente.GetAsync(e.Ruta("/campana/contador"))))
            .GetProperty("sinLeer").GetInt32();
}
