using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Personas;

/// <summary>Lecturas y comprobaciones comunes de las pruebas de fichas básicas y enlaces.</summary>
public static class LecturaDePersonas
{
    /// <summary>Cada categoría como "año: equipos", para comparar de un vistazo.</summary>
    public static List<string> CategoriasDe(JsonElement ficha) => ficha.GetProperty("categorias").EnumerateArray()
        .Select(categoria => $"{categoria.GetProperty("anio").GetInt32()}: " + string.Join(", ",
            categoria.GetProperty("equipos").EnumerateArray().Select(equipo => equipo.GetString())))
        .ToList();

    public static async Task<JsonElement> LeerAsync(ClienteDePrueba cliente, string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, $"{ruta}: {(int)respuesta.StatusCode}");
        return await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
    }

    public static async Task NoEncontradoAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    /// <summary>Comprueba que el enlace existe y lleva a esa ficha de ese tipo.</summary>
    public static void ComprobarEnlace(JsonElement enlace, Guid usuarioRolId, string tipo)
    {
        Assert.Equal(JsonValueKind.Object, enlace.ValueKind);
        Assert.Equal(usuarioRolId, enlace.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(tipo, enlace.GetProperty("tipo").GetString());
    }
}
