using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LFClub.Pruebas.Integracion.Personas;

/// <summary>
/// "Mi categoría" lleva de cada entrenador su identificador y la versión de su foto, para que la
/// familia vea su miniatura y abra su ficha básica, y nada más de su cuenta (spec 008, historia 3;
/// RF-014).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class MiCategoriaConFotoPruebas
{
    private readonly FabricaApi _fabrica;

    public MiCategoriaConFotoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Cada_entrenador_trae_su_identificador_y_la_version_de_su_foto()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Categorias.AsignarEntrenadorAsync(c.C2014, c.E2.Integrante);
        var familia = await FamiliaDeAnaAsync(c);

        var entrenadores = await EntrenadoresAsync(familia, c);

        Assert.Equal(2, entrenadores.Count);
        Assert.True(entrenadores[c.E1.Integrante.Id].GetProperty("versionFoto").GetInt32() > 0);
        Assert.Equal(0, entrenadores[c.E2.Integrante.Id].GetProperty("versionFoto").GetInt32());
        Assert.Equal("Ernesto", entrenadores[c.E1.Integrante.Id].GetProperty("nombres").GetString());
    }

    [Fact]
    public async Task Tras_quitar_E1_su_foto_la_version_es_0()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var e1 = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.E1.Usuario);
        var familia = await FamiliaDeAnaAsync(c);

        var quitada = await e1.DeleteAsync("/api/cuenta/foto");

        Assert.True(quitada.IsSuccessStatusCode, $"{(int)quitada.StatusCode}");
        Assert.Equal(0, (await EntrenadoresAsync(familia, c))[c.E1.Integrante.Id].GetProperty("versionFoto").GetInt32());
    }

    [Fact]
    public async Task Sigue_sin_documento_correo_ni_celular_de_los_entrenadores()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var familia = await FamiliaDeAnaAsync(c);

        var respuesta = await familia.GetAsync(c.Ruta("/mi-categoria"));
        var texto = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        foreach (var prohibido in new[] { c.E1.Integrante.NumeroDocumento, c.E1.Usuario.Correo, c.E1.Usuario.Celular!, "correo", "documento", "\"rol\"" })
        {
            Assert.DoesNotContain(prohibido, texto, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Un_jugador_sin_categoria_sigue_igual_que_antes()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var (cuenta, _) = await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2019);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var mia = await LeerAsync(cliente, c.Ruta("/mi-categoria"));

        Assert.Equal(JsonValueKind.Null, mia.GetProperty("categoria").ValueKind);
    }

    private Task<ClienteDePrueba> FamiliaDeAnaAsync(ClubDePersonas c) =>
        _fabrica.CrearClienteDePrueba().ElegirJugador(c.Ana.Integrante.Id).ConSesionDeAsync(c.Ana.Usuario);

    private static async Task<Dictionary<Guid, JsonElement>> EntrenadoresAsync(ClienteDePrueba familia, ClubDePersonas c) =>
        (await LeerAsync(familia, c.Ruta("/mi-categoria")))
            .GetProperty("categoria").GetProperty("entrenadores").EnumerateArray()
            .ToDictionary(entrenador => entrenador.GetProperty("usuarioRolId").GetGuid());
}
