using System.Net;
using LaPecosa.Pruebas.Integracion.Base;
using static LaPecosa.Pruebas.Integracion.Partidos.EscenarioResultadoFamilia;

namespace LaPecosa.Pruebas.Integracion.Partidos;

/// <summary>
/// "Vale el primero" con peticiones simultáneas: el bloqueo del club deja un solo marcador y una
/// suma de goles que nunca pasa del marcador (spec 010, caso límite 1, CE-002; research §3).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PrimerMarcadorPruebas : IDisposable
{
    private const int Repeticiones = 4;

    private readonly FabricaApi _fabrica;

    public PrimerMarcadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    [Fact]
    public async Task Dos_familias_a_la_vez_publican_un_solo_marcador()
    {
        var x = await CrearAsync(_fabrica);
        for (var vuelta = 0; vuelta < Repeticiones; vuelta++)
        {
            var (_, ruta) = await x.OtroPartidoAsync(_fabrica, x.E.Jn(3), x.E.Jn(4));

            var respuestas = await Task.WhenAll(
                x.E.Jn(3).Cliente.PutAsync(MiJugador(ruta), Familia(2, 1, true, 0)),
                x.E.Jn(4).Cliente.PutAsync(MiJugador(ruta), Familia(3, 0, true, 0)));

            var ganadora = Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.OK);
            var perdedora = Assert.Single(respuestas, respuesta => respuesta.StatusCode != HttpStatusCode.OK);
            await Actividades.ErrorAsync(perdedora, HttpStatusCode.Conflict, "marcador_ya_publicado");
            var publicado = await Actividades.LeerAsync(ganadora);
            var guardado = await Actividades.LeerAsync(await x.E.E1.Cliente.GetAsync(ruta));
            Assert.Equal(publicado.GetProperty("golesClub").GetInt32(), guardado.GetProperty("golesClub").GetInt32());
            Assert.Equal(publicado.GetProperty("golesRival").GetInt32(), guardado.GetProperty("golesRival").GetInt32());
        }
    }

    [Fact]
    public async Task Dos_familias_que_suman_goles_a_la_vez_no_pasan_del_marcador()
    {
        var x = await CrearAsync(_fabrica);
        for (var vuelta = 0; vuelta < Repeticiones; vuelta++)
        {
            var (partido, ruta) = await x.OtroPartidoAsync(_fabrica, x.E.Jn(3), x.E.Jn(4), x.E.Jn(5));
            await _fabrica.Actividades.PublicarPorFamiliaAsync(partido, x.E.Jn(3).Integrante, 2, 1, true, 0);

            var respuestas = await Task.WhenAll(
                x.E.Jn(4).Cliente.PutAsync(MiJugador(ruta), Familia(null, null, true, 2)),
                x.E.Jn(5).Cliente.PutAsync(MiJugador(ruta), Familia(null, null, true, 2)));

            Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.OK);
            await Actividades.ErrorAsync(
                Assert.Single(respuestas, respuesta => respuesta.StatusCode != HttpStatusCode.OK),
                HttpStatusCode.BadRequest,
                "goles_superan_marcador");
            Assert.Equal(2, await SumaDeGolesAsync(x, ruta));
        }
    }

    [Fact]
    public async Task Una_familia_y_quien_entrena_a_la_vez_dejan_el_partido_coherente()
    {
        var x = await CrearAsync(_fabrica);
        for (var vuelta = 0; vuelta < Repeticiones; vuelta++)
        {
            var (partido, ruta) = await x.OtroPartidoAsync(_fabrica, x.E.Jn(3), x.E.Jn(4));
            await _fabrica.Actividades.PublicarPorFamiliaAsync(partido, x.E.Jn(3).Integrante, 2, 1, true, 0);

            await Task.WhenAll(
                x.E.Jn(4).Cliente.PutAsync(MiJugador(ruta), Familia(null, null, true, 2)),
                x.E.E1.Cliente.PutAsync($"{ruta}/resultado", DeGestion(2, 1, (x.E.Jn(3), true, 2), (x.E.Jn(4), true, 0))));

            var detalle = await Actividades.LeerAsync(await x.E.E1.Cliente.GetAsync(ruta));
            Assert.True(await SumaDeGolesAsync(x, ruta) <= detalle.GetProperty("golesClub").GetInt32());
        }
    }

    private static async Task<int> SumaDeGolesAsync(EscenarioResultadoFamilia x, string ruta)
    {
        var planilla = await Actividades.LeerAsync(await x.E.E1.Cliente.GetAsync($"{ruta}/jugadores"));
        return planilla.GetProperty("convocados").EnumerateArray().Sum(fila => fila.GetProperty("goles").GetInt32());
    }
}
