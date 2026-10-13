using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// La convocatoria: quien gestiona el partido elige convocados entre los jugadores activos de sus
/// categorías; quien sale deja de ver el partido sin rastro y quien entra empieza a verlo (historia
/// 2, escenarios 2.5 a 2.8; RF-009, RF-012, RF-013, RF-018, RF-034).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConvocatoriaPruebas
{
    private readonly FabricaApi _fabrica;

    public ConvocatoriaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Sabado => _fabrica.Reloj.Hoy.AddDays(4);

    private static object Convocar(params Persona[] jugadores) => new { usuarioRolIds = jugadores.Select(jugador => jugador.Id) };

    private static List<Guid> Convocados(JsonElement planilla) => Actividades.Ids(planilla.GetProperty("convocados"), "usuarioRolId");

    [Fact]
    public async Task Convocar_quitar_y_agregar_cambia_quien_lo_ve_sin_dejar_rastro()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado);
        var ruta = e.Ruta($"/partidos/{partido.Id}");
        var siete = new[] { 1, 3, 4, 5, 6, 7, 8 }.Select(e.Jn).ToArray();

        var planilla = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/convocatoria", Convocar(siete)));
        Assert.Equal(7, planilla.GetProperty("convocados").GetArrayLength());
        Assert.All(planilla.GetProperty("convocados").EnumerateArray(), fila =>
            Assert.Equal(2014, fila.GetProperty("categoria").GetProperty("anio").GetInt32()));
        Assert.Equal(7, planilla.GetProperty("sinResponder").GetInt32());

        // Escenario 2.8: J8 sale y J9 entra.
        var sinJ8 = siete.Where(jugador => jugador != e.Jn(8)).Append(e.Jn(9)).ToArray();
        planilla = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/convocatoria", Convocar(sinJ8)));
        Assert.DoesNotContain(e.Jn(8).Id, Convocados(planilla));
        Assert.Contains(e.Jn(9).Id, Convocados(planilla));
        await Actividades.ErrorAsync(await e.Jn(8).Cliente.GetAsync(ruta), HttpStatusCode.NotFound, "no_encontrado");
        Assert.Equal(0, (await Actividades.LeerAsync(await e.Jn(8).Cliente.GetAsync(e.Ruta("/partidos")))).GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await e.Jn(9).Cliente.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task En_un_partido_conjunto_se_convoca_de_cualquier_categoria_pero_no_a_un_retirado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014, e.C2015], Sabado);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        var convocables = (await Actividades.LeerAsync(await e.E2.Cliente.GetAsync($"{ruta}/jugadores")))
            .GetProperty("convocables").EnumerateArray()
            .Select(fila => fila.GetProperty("categoria").GetProperty("anio").GetInt32()).Distinct().Order().ToList();
        Assert.Equal([2014, 2015], convocables);

        Assert.Equal(2, Convocados(await Actividades.LeerAsync(
            await e.E2.Cliente.PutAsync($"{ruta}/convocatoria", Convocar(e.Jn(3), e.K1)))).Count);

        // Spec 013 (RF-013): ya no hace falta ser de las categorías del partido; sí ser un jugador activo.
        Assert.Equal(3, Convocados(await Actividades.LeerAsync(
            await e.E2.Cliente.PutAsync($"{ruta}/convocatoria", Convocar(e.Jn(3), e.K1, e.M1)))).Count);
        await _fabrica.Categorias.RetirarAsync(e.Jn(5).Integrante, e.P.Integrante);
        await Actividades.ErrorAsync(
            await e.E2.Cliente.PutAsync($"{ruta}/convocatoria", Convocar(e.Jn(3), e.Jn(5))),
            HttpStatusCode.Conflict,
            "jugador_fuera_de_la_actividad");

        // Escenario 2.7: E1 entrena solo la 2014: ve la planilla y no la cambia.
        Assert.Equal(HttpStatusCode.OK, (await e.E1.Cliente.GetAsync($"{ruta}/jugadores")).StatusCode);
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync($"{ruta}/convocatoria", Convocar(e.Jn(3))), HttpStatusCode.Forbidden, "no_gestiona_actividad");
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_nunca_recibe_la_planilla()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado, convocados: [e.Jn(3).Integrante]);

        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}/jugadores")), HttpStatusCode.Forbidden, "rol_no_autorizado");
        await Actividades.ErrorAsync(
            await e.D2.Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}/jugadores")), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Un_retirado_o_uno_de_otro_club_no_se_convoca()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado);
        await _fabrica.Categorias.RetirarAsync(e.Jn(10).Integrante, e.P.Integrante);
        var ruta = e.Ruta($"/partidos/{partido.Id}/convocatoria");

        await Actividades.ErrorAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(10))), HttpStatusCode.Conflict, "jugador_fuera_de_la_actividad");
        await Actividades.ErrorAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.JugadorB)), HttpStatusCode.Conflict, "jugador_fuera_de_la_actividad");
    }

    [Fact]
    public async Task Un_convocado_que_cambia_de_categoria_sigue_convocado_con_la_categoria_copiada()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado, convocados: [e.Jn(3).Integrante]);
        await _fabrica.Actividades.CambiarCategoriaAsync(e.Jn(3).Integrante, e.C2013);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        var planilla = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/convocatoria", Convocar(e.Jn(3), e.Jn(4))));

        var j3 = planilla.GetProperty("convocados").EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == e.Jn(3).Id);
        Assert.Equal(2014, j3.GetProperty("categoria").GetProperty("anio").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await e.Jn(3).Cliente.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task Una_convocatoria_vacia_se_acepta_y_el_presidente_ve_todos_los_convocados()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var de2016 = await _fabrica.Actividades.CrearPartidoAsync([e.C2016], Sabado, convocados: [e.M1.Integrante]);
        var de2014 = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado, convocados: [e.Jn(3).Integrante]);

        Assert.Equal([e.M1.Id], Convocados(await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/partidos/{de2016.Id}/jugadores")))));
        var vacia = await Actividades.LeerAsync(
            await e.E1.Cliente.PutAsync(e.Ruta($"/partidos/{de2014.Id}/convocatoria"), Convocar()));
        Assert.Empty(Convocados(vacia));
    }
}
