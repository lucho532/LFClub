using System.Net;
using System.Text.RegularExpressions;
using LaPecosa.Pruebas.Integracion.Base;
using static LaPecosa.Pruebas.Integracion.Partidos.EscenarioResultadoFamilia;

namespace LaPecosa.Pruebas.Integracion.Partidos;

/// <summary>
/// Lo que la familia nunca recibe: ni convocados, ni quiénes jugaron, ni lo indicado por otras
/// familias, ni quién publicó (spec 010, RF-005, RF-006, escenario 1.11, CE-001). Se comprueba sobre
/// el texto de las respuestas.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class VisibilidadResultadoFamiliaPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public VisibilidadResultadoFamiliaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private static void SinNadaDeLosDemas(string texto, EscenarioResultadoFamilia x)
    {
        Assert.DoesNotContain("convocados", texto);
        Assert.DoesNotContain("indicacion", texto);
        Assert.DoesNotContain("publicacion", texto);
        Assert.DoesNotContain("origenResultado", texto);
        Assert.DoesNotContain("ultimoCambioResultado", texto);
        Assert.DoesNotContain(x.E.Jn(5).Integrante.Apellidos, texto);
        Assert.DoesNotContain(x.E.Jn(1).Integrante.Apellidos, texto);

        // "jugo" solo aparece una vez: en lo de su propio jugador.
        Assert.True(Regex.Matches(texto, "\"jugo\"").Count <= 1, texto);
    }

    [Fact]
    public async Task La_familia_solo_recibe_lo_de_su_jugador()
    {
        var x = await CrearAsync(_fabrica);
        var j3 = x.E.Jn(3).Cliente;

        var antes = await (await j3.GetAsync(x.RutaX)).Content.ReadAsStringAsync();
        SinNadaDeLosDemas(antes, x);
        Assert.Contains("\"indicaResultado\":true", antes);
        Assert.DoesNotContain(x.E.Jn(4).Integrante.Apellidos, antes);

        var publicado = await j3.PutAsync(MiJugador(x.RutaX), Familia(2, 1, true, 1));
        SinNadaDeLosDemas(await publicado.Content.ReadAsStringAsync(), x);
        await Actividades.LeerAsync(await x.E.Jn(4).Cliente.PutAsync(MiJugador(x.RutaX), Familia(null, null, true, 1)));

        // J4 ya marcó: sale como goleador (público una vez jugado), pero nada más de él.
        var despues = await (await j3.GetAsync(x.RutaX)).Content.ReadAsStringAsync();
        SinNadaDeLosDemas(despues, x);
        Assert.Contains(x.E.Jn(4).Integrante.Apellidos, despues);

        var lista = await (await j3.GetAsync(x.E.Ruta("/partidos"))).Content.ReadAsStringAsync();
        SinNadaDeLosDemas(lista, x);
        Assert.DoesNotContain("miParticipacion", lista);

        Assert.Equal(HttpStatusCode.Forbidden, (await j3.GetAsync($"{x.RutaX}/jugadores")).StatusCode);
    }

    [Fact]
    public async Task Los_demas_ven_el_partido_jugado_sin_saber_quien_lo_publico()
    {
        var x = await CrearAsync(_fabrica);
        await Actividades.LeerAsync(await x.E.Jn(3).Cliente.PutAsync(MiJugador(x.RutaX), Familia(2, 1, true, 1)));

        foreach (var persona in new[] { x.E.Jn(9), x.E.D2 })
        {
            var respuesta = await persona.Cliente.GetAsync(x.RutaX);
            var texto = await respuesta.Content.ReadAsStringAsync();
            var detalle = await Actividades.LeerAsync(respuesta);
            Assert.Equal(2, detalle.GetProperty("golesClub").GetInt32());
            Assert.Single(detalle.GetProperty("goleadores").EnumerateArray());
            Assert.DoesNotContain("publicacion", texto);
            Assert.DoesNotContain("origenResultado", texto);
            Assert.DoesNotContain("miParticipacion", texto);
            Assert.DoesNotContain("convocados", texto);
            Assert.Contains("\"indicaResultado\":false", texto);
        }
    }

    [Fact]
    public async Task El_motivo_de_goles_de_mas_no_nombra_a_nadie()
    {
        var x = await CrearAsync(_fabrica);
        await Actividades.LeerAsync(await x.E.Jn(3).Cliente.PutAsync(MiJugador(x.RutaX), Familia(2, 1, true, 1)));

        var rechazo = await x.E.Jn(4).Cliente.PutAsync(MiJugador(x.RutaX), Familia(null, null, true, 2));

        var texto = await rechazo.Content.ReadAsStringAsync();
        await Actividades.ErrorAsync(rechazo, HttpStatusCode.BadRequest, "goles_superan_marcador");
        Assert.DoesNotContain(x.E.Jn(3).Integrante.Apellidos, texto);
        Assert.DoesNotContain(x.E.Jn(3).Integrante.Nombres, texto);
    }
}
