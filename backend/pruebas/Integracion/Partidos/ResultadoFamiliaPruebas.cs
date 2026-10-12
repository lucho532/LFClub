using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Partidos.EscenarioResultadoFamilia;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// La familia de un convocado publica el resultado y lo de su jugador (spec 010, historia 1,
/// escenarios 1.1 a 1.7 y 1.12; RF-001 a RF-012).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ResultadoFamiliaPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public ResultadoFamiliaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private static async Task<JsonElement> CifrasAsync(EscenarioResultadoFamilia x, Persona jugador) =>
        (await Actividades.LeerAsync(await x.E.P.Cliente.GetAsync(x.E.Ruta($"/jugadores/{jugador.Id}/estadisticas")))).GetProperty("total");

    [Fact]
    public async Task Antes_de_empezar_no_hay_formulario_ni_se_publica()
    {
        var x = await CrearAsync(_fabrica);

        var y = await Actividades.LeerAsync(await x.E.Jn(3).Cliente.GetAsync(x.RutaY));
        Assert.False(y.GetProperty("permisos").GetProperty("indicaResultado").GetBoolean());
        Assert.False(y.TryGetProperty("miParticipacion", out _));
        await Actividades.ErrorAsync(
            await x.E.Jn(3).Cliente.PutAsync(MiJugador(x.RutaY), Familia(1, 0, true, 0)), HttpStatusCode.Conflict, "partido_sin_empezar");
    }

    [Fact]
    public async Task Quien_no_esta_convocado_no_encuentra_el_partido_ni_jugado()
    {
        var x = await CrearAsync(_fabrica);
        var j6 = x.E.Jn(6).Cliente;

        await Actividades.ErrorAsync(await j6.GetAsync(x.RutaX), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(await j6.PutAsync(MiJugador(x.RutaX), Familia(1, 0, false, 0)), HttpStatusCode.NotFound, "no_encontrado");

        await Actividades.LeerAsync(await x.E.Jn(3).Cliente.PutAsync(MiJugador(x.RutaX), Familia(2, 1, true, 1)));
        await Actividades.LeerAsync(await j6.GetAsync(x.RutaX));
        await Actividades.ErrorAsync(await j6.PutAsync(MiJugador(x.RutaX), Familia(null, null, false, 0)), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task La_familia_publica_y_las_estadisticas_cuentan_al_instante()
    {
        var x = await CrearAsync(_fabrica);
        var ruta = MiJugador(x.RutaX);
        await Actividades.ErrorAsync(await x.E.Jn(3).Cliente.PutAsync(ruta, Familia(2, 1, false, 1)), HttpStatusCode.BadRequest, "goles_de_quien_no_jugo");
        await Actividades.ErrorAsync(await x.E.Jn(3).Cliente.PutAsync(ruta, Familia(2, null, true, 1)), HttpStatusCode.BadRequest, "falta_marcador");

        var publicado = await Actividades.LeerAsync(await x.E.Jn(3).Cliente.PutAsync(ruta, Familia(2, 1, true, 1)));

        Assert.Equal("JUGADO", publicado.GetProperty("estado").GetString());
        var mia = publicado.GetProperty("miParticipacion");
        Assert.True(mia.GetProperty("jugo").GetBoolean());
        Assert.Equal(1, mia.GetProperty("goles").GetInt32());
        Assert.Equal("FAMILIA", mia.GetProperty("indicadoPor").GetString());
        Assert.True(mia.GetProperty("puedeCambiar").GetBoolean());
        var cifras = await CifrasAsync(x, x.E.Jn(3));
        Assert.Equal(1, cifras.GetProperty("partidosJugados").GetInt32());
        Assert.Equal(1, cifras.GetProperty("goles").GetInt32());

        // RF-011: J5, sin indicar nada, no cuenta como que jugó.
        Assert.Equal(0, (await CifrasAsync(x, x.E.Jn(5))).GetProperty("partidosJugados").GetInt32());
        var j5 = await Actividades.LeerAsync(await x.E.Jn(5).Cliente.GetAsync(x.RutaX));
        Assert.False(j5.GetProperty("miParticipacion").TryGetProperty("indicadoPor", out _));
    }

    [Fact]
    public async Task Las_demas_familias_indican_lo_suyo_sin_cambiar_el_marcador()
    {
        var x = await CrearAsync(_fabrica);
        var ruta = MiJugador(x.RutaX);
        await Actividades.LeerAsync(await x.E.Jn(3).Cliente.PutAsync(ruta, Familia(2, 1, true, 1)));

        // Supuesto 3: también el mismo marcador.
        await Actividades.ErrorAsync(await x.E.Jn(4).Cliente.PutAsync(ruta, Familia(3, 0, true, 0)), HttpStatusCode.Conflict, "marcador_ya_publicado");
        await Actividades.ErrorAsync(await x.E.Jn(4).Cliente.PutAsync(ruta, Familia(2, 1, true, 0)), HttpStatusCode.Conflict, "marcador_ya_publicado");
        await Actividades.ErrorAsync(await x.E.Jn(4).Cliente.PutAsync(ruta, Familia(null, null, true, 2)), HttpStatusCode.BadRequest, "goles_superan_marcador");
        var j4 = await Actividades.LeerAsync(await x.E.Jn(4).Cliente.PutAsync(ruta, Familia(null, null, true, 1)));
        Assert.Equal(2, j4.GetProperty("golesClub").GetInt32());
        Assert.Equal(1, j4.GetProperty("miParticipacion").GetProperty("goles").GetInt32());

        // Escenario 1.12: J3 cambia lo suyo.
        var j3 = await Actividades.LeerAsync(await x.E.Jn(3).Cliente.PutAsync(ruta, Familia(null, null, true, 0)));
        Assert.Equal(0, j3.GetProperty("miParticipacion").GetProperty("goles").GetInt32());
        Assert.Equal(1, (await CifrasAsync(x, x.E.Jn(3))).GetProperty("partidosJugados").GetInt32());
    }

    [Fact]
    public async Task Un_partido_cancelado_se_rechaza()
    {
        var x = await CrearAsync(_fabrica);
        var z = await _fabrica.Actividades.CrearPartidoAsync(
            [x.E.C2014], x.X.Fecha, new TimeOnly(8, 0), [x.E.Jn(3).Integrante], EstadoPartido.CANCELADO);
        var rutaZ = x.E.Ruta($"/partidos/{z.Id}");

        Assert.False((await Actividades.LeerAsync(await x.E.Jn(3).Cliente.GetAsync(rutaZ))).TryGetProperty("miParticipacion", out _));
        await Actividades.ErrorAsync(
            await x.E.Jn(3).Cliente.PutAsync(MiJugador(rutaZ), Familia(1, 0, true, 0)), HttpStatusCode.Conflict, "actividad_cancelada");
    }

    [Fact]
    public async Task Con_dos_hermanos_cuenta_el_jugador_elegido()
    {
        var x = await CrearAsync(_fabrica);

        var conJ1 = await Actividades.LeerAsync(await x.E.Jn(1).Cliente.GetAsync(x.RutaX));
        Assert.True(conJ1.GetProperty("permisos").GetProperty("indicaResultado").GetBoolean());
        await Actividades.ErrorAsync(await x.E.Jn(2).Cliente.GetAsync(x.RutaX), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await x.E.Jn(2).Cliente.PutAsync(MiJugador(x.RutaX), Familia(1, 0, false, 0)), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Los_demas_roles_no_publican_por_aqui()
    {
        var x = await CrearAsync(_fabrica);

        foreach (var persona in new[] { x.E.E1, x.E.D1, x.E.P })
        {
            await Actividades.ErrorAsync(
                await persona.Cliente.PutAsync(MiJugador(x.RutaX), Familia(1, 0, false, 0)), HttpStatusCode.Forbidden, "rol_no_autorizado");
        }

        await Actividades.ErrorAsync(
            await x.E.PB.Cliente.PutAsync(MiJugador(x.RutaX), Familia(1, 0, false, 0)), HttpStatusCode.NotFound, "no_encontrado");
    }
}
