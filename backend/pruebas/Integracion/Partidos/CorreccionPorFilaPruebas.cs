using System.Net;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Partidos.EscenarioResultadoFamilia;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// Quien entrena o el PRESIDENTE ven quién publicó y quién indicó cada cosa, y corrigen fila por fila
/// (spec 010, escenarios 1.8 a 1.12, caso límite 2, RF-009, RF-012, CE-003; research §2, supuesto 1).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CorreccionPorFilaPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public CorreccionPorFilaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private static string Nombre(Persona persona) => $"{persona.Integrante.Nombres} {persona.Integrante.Apellidos}";

    // J3 publica 2-1 con un gol y J4 indica un gol; J1 y J5 sin indicar.
    private async Task<EscenarioResultadoFamilia> PublicadoAsync()
    {
        var x = await CrearAsync(_fabrica);
        await Actividades.LeerAsync(await x.E.Jn(3).Cliente.PutAsync(MiJugador(x.RutaX), Familia(2, 1, true, 1)));
        await Actividades.LeerAsync(await x.E.Jn(4).Cliente.PutAsync(MiJugador(x.RutaX), Familia(null, null, true, 1)));
        return x;
    }

    [Fact]
    public async Task Quien_entrena_ve_quien_publico_y_quien_indico_cada_fila()
    {
        var x = await PublicadoAsync();

        var detalle = await Actividades.LeerAsync(await x.E.E1.Cliente.GetAsync(x.RutaX));
        var publicacion = detalle.GetProperty("publicacion");
        Assert.Equal(Nombre(x.E.Jn(3)), publicacion.GetProperty("autor").GetString());
        Assert.True(publicacion.GetProperty("porFamilia").GetBoolean());
        Assert.Equal("FAMILIA", detalle.GetProperty("origenResultado").GetString());

        var j3 = await x.FilaAsync(x.RutaX, x.E.Jn(3));
        Assert.Equal("FAMILIA", j3.GetProperty("indicacion").GetProperty("origen").GetString());
        Assert.Equal(Nombre(x.E.Jn(3)), j3.GetProperty("indicacion").GetProperty("autor").GetString());
        Assert.Equal(Nombre(x.E.Jn(4)), (await x.FilaAsync(x.RutaX, x.E.Jn(4))).GetProperty("indicacion").GetProperty("autor").GetString());
        Assert.False((await x.FilaAsync(x.RutaX, x.E.Jn(5))).TryGetProperty("indicacion", out _));
    }

    [Fact]
    public async Task Corregir_solo_cierra_las_filas_que_cambian()
    {
        var x = await PublicadoAsync();

        var corregido = await Actividades.LeerAsync(await x.E.E1.Cliente.PutAsync(
            $"{x.RutaX}/resultado", DeGestion(3, 1, (x.E.Jn(3), true, 1), (x.E.Jn(4), true, 2))));

        Assert.Equal("GESTION", corregido.GetProperty("origenResultado").GetString());
        Assert.Equal(Nombre(x.E.Jn(3)), corregido.GetProperty("publicacion").GetProperty("autor").GetString());
        Assert.Equal([2, 1], corregido.GetProperty("goleadores").EnumerateArray().Select(fila => fila.GetProperty("goles").GetInt32()));
        var j4 = (await x.FilaAsync(x.RutaX, x.E.Jn(4))).GetProperty("indicacion");
        Assert.Equal("GESTION", j4.GetProperty("origen").GetString());
        Assert.Equal(Nombre(x.E.E1), j4.GetProperty("autor").GetString());
        Assert.Equal("FAMILIA", (await x.FilaAsync(x.RutaX, x.E.Jn(3))).GetProperty("indicacion").GetProperty("origen").GetString());

        // CE-003: las estadísticas reflejan la corrección al instante.
        var cifras = await Actividades.LeerAsync(await x.E.P.Cliente.GetAsync(x.E.Ruta($"/jugadores/{x.E.Jn(4).Id}/estadisticas")));
        Assert.Equal(2, cifras.GetProperty("total").GetProperty("goles").GetInt32());

        // J3, cuya fila no cambió, todavía puede cambiar lo suyo; J4 ya no.
        await Actividades.LeerAsync(await x.E.Jn(3).Cliente.PutAsync(MiJugador(x.RutaX), Familia(null, null, true, 0)));
        await Actividades.ErrorAsync(
            await x.E.Jn(4).Cliente.PutAsync(MiJugador(x.RutaX), Familia(null, null, true, 1)), HttpStatusCode.Conflict, "indicacion_corregida");
        var paraJ4 = await Actividades.LeerAsync(await x.E.Jn(4).Cliente.GetAsync(x.RutaX));
        Assert.False(paraJ4.GetProperty("miParticipacion").GetProperty("puedeCambiar").GetBoolean());
        Assert.False(paraJ4.GetProperty("permisos").GetProperty("indicaResultado").GetBoolean());
    }

    [Fact]
    public async Task Bajar_el_marcador_sin_quitar_goles_se_rechaza()
    {
        var x = await PublicadoAsync();

        await Actividades.ErrorAsync(
            await x.E.E1.Cliente.PutAsync($"{x.RutaX}/resultado", DeGestion(1, 1, (x.E.Jn(3), true, 1), (x.E.Jn(4), true, 1))),
            HttpStatusCode.BadRequest,
            "goles_superan_marcador");
    }

    [Fact]
    public async Task Marcar_como_que_jugo_a_quien_nadie_indico_lo_corrige_y_tambien_el_presidente_puede()
    {
        var x = await PublicadoAsync();

        await Actividades.LeerAsync(await x.E.P.Cliente.PutAsync(
            $"{x.RutaX}/resultado", DeGestion(2, 1, (x.E.Jn(3), true, 1), (x.E.Jn(4), true, 1), (x.E.Jn(5), true, 0))));

        var j5 = (await x.FilaAsync(x.RutaX, x.E.Jn(5))).GetProperty("indicacion");
        Assert.Equal("GESTION", j5.GetProperty("origen").GetString());
        Assert.Equal(Nombre(x.E.P), j5.GetProperty("autor").GetString());
        Assert.False((await x.FilaAsync(x.RutaX, x.E.Jn(1))).TryGetProperty("indicacion", out _));
        Assert.Equal("FAMILIA", (await x.FilaAsync(x.RutaX, x.E.Jn(4))).GetProperty("indicacion").GetProperty("origen").GetString());
    }

    [Fact]
    public async Task Registrar_sin_familia_no_es_publicacion_de_una_familia()
    {
        var x = await CrearAsync(_fabrica);

        var registrado = await Actividades.LeerAsync(await x.E.E1.Cliente.PutAsync(
            $"{x.RutaX}/resultado", DeGestion(1, 0, (x.E.Jn(3), true, 1))));

        Assert.Equal("GESTION", registrado.GetProperty("origenResultado").GetString());
        Assert.False(registrado.GetProperty("publicacion").GetProperty("porFamilia").GetBoolean());
        Assert.Equal(Nombre(x.E.E1), registrado.GetProperty("publicacion").GetProperty("autor").GetString());

        // La familia, después, ya no publica marcador.
        await Actividades.ErrorAsync(
            await x.E.Jn(4).Cliente.PutAsync(MiJugador(x.RutaX), Familia(2, 0, false, 0)), HttpStatusCode.Conflict, "marcador_ya_publicado");
    }
}
