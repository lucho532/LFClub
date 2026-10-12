using System.Net;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// Corregir el rival de un partido ya jugado: solo quien gestiona el partido lo hace, el marcador y
/// los goles quedan como estaban, y en un partido que no se ha jugado no aplica (RF-008 y RF-016).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RivalDePartidoJugadoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public RivalDePartidoJugadoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private async Task<(EscenarioActividades E, string Ruta)> JugadoAsync()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var sabado = _fabrica.Reloj.Hoy.AddDays(4);
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], sabado, new TimeOnly(9, 0), [e.Jn(3).Integrante, e.Jn(4).Integrante]);
        _fabrica.Reloj.FijarEnColombia(sabado, new TimeOnly(10, 0));
        var ruta = e.Ruta($"/partidos/{partido.Id}");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/resultado", new
        {
            golesClub = 2,
            golesRival = 1,
            jugadores = new[]
            {
                new { usuarioRolId = e.Jn(3).Id, jugo = true, goles = 2 },
                new { usuarioRolId = e.Jn(4).Id, jugo = true, goles = 0 },
            },
        }));
        return (e, ruta);
    }

    [Fact]
    public async Task Quien_gestiona_corrige_el_rival_y_el_marcador_no_cambia()
    {
        var (e, ruta) = await JugadoAsync();

        var corregido = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/rival", new { rival = "  Los Tigres  " }));

        Assert.Equal("JUGADO", corregido.GetProperty("estado").GetString());
        Assert.Equal("Los Tigres", corregido.GetProperty("rival").GetString());
        Assert.Equal(2, corregido.GetProperty("golesClub").GetInt32());
        Assert.Equal(1, corregido.GetProperty("golesRival").GetInt32());
        Assert.Equal("Los Tigres", (await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(ruta))).GetProperty("rival").GetString());
    }

    [Fact]
    public async Task Sin_nombre_queda_por_definir_y_uno_muy_largo_se_rechaza()
    {
        var (e, ruta) = await JugadoAsync();

        var vacio = await Actividades.LeerAsync(await e.P.Cliente.PutAsync($"{ruta}/rival", new { rival = "   " }));
        Assert.False(vacio.TryGetProperty("rival", out var rival) && rival.ValueKind != System.Text.Json.JsonValueKind.Null);

        await Actividades.ErrorAsync(
            await e.P.Cliente.PutAsync($"{ruta}/rival", new { rival = new string('a', 500) }), HttpStatusCode.BadRequest, "datos_invalidos");
    }

    [Fact]
    public async Task Quien_no_gestiona_el_partido_no_lo_corrige()
    {
        var (e, ruta) = await JugadoAsync();

        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.PutAsync($"{ruta}/rival", new { rival = "Otro" }), HttpStatusCode.Forbidden, "rol_no_autorizado");
        await Actividades.ErrorAsync(
            await e.D2.Cliente.PutAsync($"{ruta}/rival", new { rival = "Otro" }), HttpStatusCode.Forbidden, "no_gestiona_actividad");
    }

    [Fact]
    public async Task En_un_partido_proximo_no_aplica()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], _fabrica.Reloj.Hoy.AddDays(4));

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(e.Ruta($"/partidos/{partido.Id}/rival"), new { rival = "Otro" }),
            HttpStatusCode.Conflict, "partido_no_jugado");
    }
}
