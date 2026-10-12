using System.Net;
using System.Text.Json;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Estadisticas;

/// <summary>
/// Los goleadores por categoría: los goles cuentan para la categoría con la que se marcaron,
/// retirados incluidos, y solo se muestra nombre, goles y enlace (historia 6, escenarios 6.3 a 6.5;
/// RF-029).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class GoleadoresPruebas
{
    private readonly FabricaApi _fabrica;

    public GoleadoresPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private async Task<EscenarioActividades> ConGolesAsync()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var hoy = _fabrica.Reloj.Hoy;
        var (j3, j4) = (e.Jn(3).Integrante, e.Jn(4).Integrante);
        var primero = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], hoy.AddDays(-7), convocados: [j3, j4]);
        await _fabrica.Actividades.RegistrarJugadoAsync(primero, 3, 1, (j3, 2), (j4, 1));
        var segundo = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], hoy.AddDays(-3), convocados: [j3]);
        await _fabrica.Actividades.RegistrarJugadoAsync(segundo, 1, 0, (j3, 1));
        return e;
    }

    private static List<(string Apellidos, int Goles)> Lista(JsonElement goleadores) => goleadores.GetProperty("goleadores")
        .EnumerateArray().Select(fila => (fila.GetProperty("apellidos").GetString()!, fila.GetProperty("goles").GetInt32())).ToList();

    [Fact]
    public async Task Sin_categoria_trae_las_que_tienen_goles_y_con_ella_los_goleadores_de_mayor_a_menor()
    {
        var e = await ConGolesAsync();

        var sinCategoria = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta("/goleadores")));
        Assert.Equal([2014], sinCategoria.GetProperty("categoriasConGoles").EnumerateArray().Select(fila => fila.GetProperty("anio").GetInt32()));
        Assert.Equal(0, sinCategoria.GetProperty("goleadores").GetArrayLength());

        var de2014 = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/goleadores?categoriaId={e.C2014.Id}")));
        Assert.Equal([(e.Jn(3).Integrante.Apellidos, 3), (e.Jn(4).Integrante.Apellidos, 1)], Lista(de2014));
    }

    [Fact]
    public async Task Los_goles_siguen_en_su_categoria_aunque_el_jugador_cambie_o_se_retire()
    {
        var e = await ConGolesAsync();
        await _fabrica.Actividades.CambiarCategoriaAsync(e.Jn(3).Integrante, e.C2013);
        await _fabrica.Categorias.RetirarAsync(e.Jn(4).Integrante, e.P.Integrante);

        var de2014 = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/goleadores?categoriaId={e.C2014.Id}")));
        var de2013 = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/goleadores?categoriaId={e.C2013.Id}")));

        Assert.Equal(2, Lista(de2014).Count);
        Assert.Empty(Lista(de2013));
    }

    [Fact]
    public async Task Solo_dice_nombre_y_goles_y_la_cuenta_de_un_jugador_no_recibe_enlaces()
    {
        var e = await ConGolesAsync();
        var ruta = e.Ruta($"/goleadores?categoriaId={e.C2014.Id}");

        var texto = await (await e.Jn(5).Cliente.GetAsync(ruta)).Content.ReadAsStringAsync();
        foreach (var dato in new[] { "numeroDocumento", "fechaNacimiento", "categoriaId\":\"" + e.C2013.Id, e.Jn(3).Integrante.NumeroDocumento })
        {
            Assert.DoesNotContain(dato, texto);
        }

        var deJ5 = JsonDocument.Parse(texto).RootElement;
        Assert.All(deJ5.GetProperty("goleadores").EnumerateArray(), fila =>
            Assert.Equal(JsonValueKind.Null, fila.GetProperty("enlace").ValueKind));
        var deP = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(ruta));
        Assert.All(deP.GetProperty("goleadores").EnumerateArray(), fila =>
            Assert.Equal("JUGADOR", fila.GetProperty("enlace").GetProperty("tipo").GetString()));

        foreach (var persona in new[] { e.Jn(10), e.D2 })
        {
            Assert.Equal(Lista(deP), Lista(await Actividades.LeerAsync(await persona.Cliente.GetAsync(ruta))));
        }
    }

    [Fact]
    public async Task Otro_club_o_una_categoria_ajena_responde_404()
    {
        var e = await ConGolesAsync();

        await Actividades.ErrorAsync(await e.PB.Cliente.GetAsync(e.Ruta("/goleadores")), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.P.Cliente.GetAsync(e.Ruta($"/goleadores?categoriaId={e.CB.Id}")), HttpStatusCode.NotFound, "no_encontrado");
    }
}
