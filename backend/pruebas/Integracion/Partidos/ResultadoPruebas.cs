using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Partidos;

/// <summary>
/// El resultado: se registra cuando el partido ya empezó, con un marcador válido; el partido jugado
/// lo ve todo el club con sus goleadores y sin convocados; corregirlo deja quién y cuándo (historia
/// 3, escenarios 3.1 a 3.8; RF-015 a RF-021; CE-002, CE-004).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ResultadoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public ResultadoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private static object Resultado(int club, int rival, params (Persona Jugador, bool Jugo, int Goles)[] filas) => new
    {
        golesClub = club,
        golesRival = rival,
        jugadores = filas.Select(fila => new { usuarioRolId = fila.Jugador.Id, jugo = fila.Jugo, goles = fila.Goles }),
    };

    private async Task<(EscenarioActividades E, Partido Partido, string Ruta)> EmpezadoAsync(TimeOnly? ahora = null)
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var sabado = _fabrica.Reloj.Hoy.AddDays(4);
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], sabado, new TimeOnly(9, 0), new[] { 1, 3, 4, 5, 6, 7, 8 }.Select(numero => e.Jn(numero).Integrante));
        _fabrica.Reloj.FijarEnColombia(sabado, ahora ?? new TimeOnly(10, 0));
        return (e, partido, e.Ruta($"/partidos/{partido.Id}"));
    }

    [Fact]
    public async Task Antes_de_la_hora_no_se_registra()
    {
        var (e, _, ruta) = await EmpezadoAsync(new TimeOnly(8, 59));

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync($"{ruta}/resultado", Resultado(1, 0)), HttpStatusCode.Conflict, "partido_sin_empezar");
    }

    [Fact]
    public async Task Los_resultados_imposibles_se_rechazan_y_menos_goles_de_jugadores_se_acepta()
    {
        var (e, _, ruta) = await EmpezadoAsync();
        var resultado = $"{ruta}/resultado";

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(resultado, Resultado(3, 1, (e.Jn(3), false, 1))), HttpStatusCode.BadRequest, "goles_de_quien_no_jugo");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(resultado, Resultado(3, 1, (e.Jn(3), true, 3), (e.Jn(4), true, 1))),
            HttpStatusCode.BadRequest, "goles_superan_marcador");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(resultado, Resultado(1, 0, (e.Jn(9), true, 1))), HttpStatusCode.BadRequest, "jugador_no_convocado");
        Assert.Equal("PROXIMO", (await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(ruta))).GetProperty("estado").GetString());

        var aceptado = await Actividades.LeerAsync(
            await e.E1.Cliente.PutAsync(resultado, Resultado(3, 2, (e.Jn(3), true, 1), (e.Jn(4), true, 1))));
        Assert.Equal("JUGADO", aceptado.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Jugado_lo_ve_todo_el_club_con_sus_goleadores_y_sin_convocados()
    {
        var (e, partido, ruta) = await EmpezadoAsync();
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{ruta}/resultado", Resultado(3, 1, (e.Jn(3), true, 2), (e.Jn(4), true, 1), (e.Jn(5), true, 0))));

        foreach (var persona in new[] { e.Jn(10), e.D2, e.M1, e.P, e.Jn(3) })
        {
            var detalle = await Actividades.LeerAsync(await persona.Cliente.GetAsync(ruta));
            Assert.Equal(3, detalle.GetProperty("golesClub").GetInt32());
            Assert.Equal(1, detalle.GetProperty("golesRival").GetInt32());
            var goleadores = detalle.GetProperty("goleadores").EnumerateArray().ToList();
            Assert.Equal([2, 1], goleadores.Select(fila => fila.GetProperty("goles").GetInt32()));
            Assert.Contains(partido.Id, Actividades.Ids(await Actividades.LeerAsync(await persona.Cliente.GetAsync(e.Ruta("/partidos"))), "partidoId"));
        }

        // Escenario 3.6: la cuenta de un jugador no sabe quién más jugó ni quién fue convocado.
        foreach (var persona in new[] { e.Jn(10), e.Jn(3) })
        {
            var texto = await (await persona.Cliente.GetAsync(ruta)).Content.ReadAsStringAsync();
            Assert.DoesNotContain("convocados", texto);
            Assert.DoesNotContain("jugo", texto);
            Assert.DoesNotContain("ultimoCambioResultado", texto);
            Assert.DoesNotContain(e.Jn(5).Integrante.Apellidos, texto);
            Assert.DoesNotContain("\"tipo\":\"JUGADOR\"", texto);
        }

        await Actividades.ErrorAsync(await e.PB.Cliente.GetAsync(ruta), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Corregir_cambia_los_goleadores_y_dice_quien_y_cuando()
    {
        var (e, _, ruta) = await EmpezadoAsync();
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/resultado", Resultado(3, 1, (e.Jn(3), true, 2), (e.Jn(4), true, 1))));

        var corregido = await Actividades.LeerAsync(await e.E2.Cliente.PutAsync(
            $"{ruta}/resultado", Resultado(3, 2, (e.Jn(3), true, 2), (e.Jn(5), true, 1))));

        Assert.Equal(2, corregido.GetProperty("golesRival").GetInt32());
        var apellidos = corregido.GetProperty("goleadores").EnumerateArray().Select(fila => fila.GetProperty("apellidos").GetString()).ToList();
        Assert.Equal([e.Jn(3).Integrante.Apellidos, e.Jn(5).Integrante.Apellidos], apellidos);
        var cambio = corregido.GetProperty("ultimoCambioResultado");
        Assert.Equal($"{e.E2.Integrante.Nombres} {e.E2.Integrante.Apellidos}", cambio.GetProperty("autor").GetString());
        Assert.Equal(e.E2.Id, cambio.GetProperty("autorFicha").GetProperty("usuarioRolId").GetGuid());
        var planilla = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync($"{ruta}/jugadores"));
        var j4 = planilla.GetProperty("convocados").EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == e.Jn(4).Id);
        Assert.False(j4.GetProperty("jugo").GetBoolean());
        Assert.Equal(0, j4.GetProperty("goles").GetInt32());
    }

    [Fact]
    public async Task Un_cambio_de_categoria_no_cambia_el_partido_ni_sus_goles()
    {
        var (e, _, ruta) = await EmpezadoAsync();
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/resultado", Resultado(1, 0, (e.Jn(3), true, 1))));

        await _fabrica.Actividades.CambiarCategoriaAsync(e.Jn(3).Integrante, e.C2013);

        var detalle = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(ruta));
        Assert.Equal([2014], Actividades.Anios(detalle));
        var planilla = await Actividades.LeerAsync(await e.P.Cliente.GetAsync($"{ruta}/jugadores"));
        var j3 = planilla.GetProperty("convocados").EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == e.Jn(3).Id);
        Assert.Equal(2014, j3.GetProperty("categoria").GetProperty("anio").GetInt32());
        Assert.Equal(1, j3.GetProperty("goles").GetInt32());
    }

    [Fact]
    public async Task Un_partido_jugado_no_se_cancela_ni_cambia_sus_datos_ni_su_convocatoria()
    {
        var (e, _, ruta) = await EmpezadoAsync();
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/resultado", Resultado(1, 0)));
        var sabado = _fabrica.Reloj.Hoy;

        await Actividades.ErrorAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"), HttpStatusCode.Conflict, "partido_jugado");
        await Actividades.ErrorAsync(await e.E1.Cliente.DeleteAsync($"{ruta}/cancelacion"), HttpStatusCode.Conflict, "actividad_no_cancelada");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Partido(sabado, [e.C2014])), HttpStatusCode.Conflict, "partido_jugado");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync($"{ruta}/convocatoria", new { usuarioRolIds = new[] { e.Jn(3).Id } }),
            HttpStatusCode.Conflict, "partido_jugado");
    }
}
