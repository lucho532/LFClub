using System.Net;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Partidos;

/// <summary>
/// Crear un partido: lo crea el PRESIDENTE o quien entrena todas sus categorías, nace próximo y
/// sin convocados, el torneo exige su nombre y el rival puede quedar por definir (historia 2,
/// escenario 2.10; RF-007, RF-008).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CrearPartidoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public CrearPartidoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Sabado => _fabrica.Reloj.Hoy.AddDays(4);

    [Fact]
    public async Task Quien_entrena_la_categoria_crea_un_partido_proximo_sin_convocados()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var creado = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/partidos"), Actividades.Partido(Sabado, [e.C2014])), HttpStatusCode.Created);

        Assert.Equal("PROXIMO", creado.GetProperty("estado").GetString());
        Assert.False(creado.GetProperty("faltaResultado").GetBoolean());
        Assert.Equal("09:00", creado.GetProperty("hora").GetString());
        Assert.False(creado.TryGetProperty("nombreTorneo", out _));
        var planilla = await Actividades.LeerAsync(
            await e.E1.Cliente.GetAsync(e.Ruta($"/partidos/{creado.GetProperty("partidoId").GetGuid()}/jugadores")));
        Assert.Equal(0, planilla.GetProperty("convocados").GetArrayLength());
    }

    [Fact]
    public async Task Un_torneo_exige_su_nombre_y_un_amistoso_lo_descarta()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var ruta = e.Ruta("/partidos");

        var sinNombre = await e.E2.Cliente.PostAsync(ruta, Actividades.Partido(Sabado, [e.C2014, e.C2015], tipo: "TORNEO"));
        await Actividades.ErrorAsync(sinNombre, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True((await Actividades.LeerAsync(sinNombre, HttpStatusCode.BadRequest)).GetProperty("errores").TryGetProperty("nombreTorneo", out _));

        var conNombre = await Actividades.LeerAsync(
            await e.E2.Cliente.PostAsync(ruta, Actividades.Partido(Sabado, [e.C2014, e.C2015], tipo: "TORNEO", nombreTorneo: "Copa Ciudad")),
            HttpStatusCode.Created);
        Assert.Equal("Copa Ciudad", conNombre.GetProperty("nombreTorneo").GetString());

        var amistoso = await Actividades.LeerAsync(
            await e.E2.Cliente.PostAsync(ruta, Actividades.Partido(Sabado, [e.C2014], nombreTorneo: "Copa Ciudad")),
            HttpStatusCode.Created);
        Assert.False(amistoso.TryGetProperty("nombreTorneo", out _));
    }

    [Fact]
    public async Task El_rival_se_puede_dejar_por_definir_y_escribir_despues()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var sinRival = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/partidos"), Actividades.Partido(Sabado, [e.C2014], rival: null)),
            HttpStatusCode.Created);
        Assert.False(sinRival.TryGetProperty("rival", out _));
        var ruta = e.Ruta($"/partidos/{sinRival.GetProperty("partidoId").GetGuid()}");
        var enBlanco = await Actividades.LeerAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Partido(Sabado, [e.C2014], rival: "   ")));
        Assert.False(enBlanco.TryGetProperty("rival", out _));

        var conRival = await Actividades.LeerAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Partido(Sabado, [e.C2014], rival: "Equipo D")));
        Assert.Equal("Equipo D", conRival.GetProperty("rival").GetString());
        var largo = await e.E1.Cliente.PutAsync(ruta, Actividades.Partido(Sabado, [e.C2014], rival: new string('x', 121)));
        await Actividades.ErrorAsync(largo, HttpStatusCode.BadRequest, "datos_invalidos");
    }

    [Fact]
    public async Task Solo_lo_crea_el_presidente_o_quien_entrena_todas_sus_categorias()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var ruta = e.Ruta("/partidos");

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PostAsync(ruta, Actividades.Partido(Sabado, [e.C2014, e.C2015])),
            HttpStatusCode.Forbidden, "no_gestiona_actividad");
        Assert.Equal(HttpStatusCode.Created, (await e.E2.Cliente.PostAsync(ruta, Actividades.Partido(Sabado, [e.C2014, e.C2015]))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await e.P.Cliente.PostAsync(ruta, Actividades.Partido(Sabado, [e.C2016]))).StatusCode);
        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.PostAsync(ruta, Actividades.Partido(Sabado, [e.C2014])), HttpStatusCode.Forbidden, "rol_no_autorizado");

        var inactiva = await _fabrica.Categorias.CrearCategoriaAsync(e.Club, 2012, activa: false);
        await Actividades.ErrorAsync(
            await e.P.Cliente.PostAsync(ruta, Actividades.Partido(Sabado, [inactiva])), HttpStatusCode.Conflict, "categoria_inactiva");
    }

    [Fact]
    public async Task Sin_convocados_solo_lo_ven_quienes_lo_entrenan_y_el_presidente()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        foreach (var persona in new[] { e.E1, e.E2, e.D1, e.P })
        {
            Assert.Equal(HttpStatusCode.OK, (await persona.Cliente.GetAsync(ruta)).StatusCode);
        }

        foreach (var persona in new[] { e.D2, e.Jn(3), e.Jn(1) })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await persona.Cliente.GetAsync(ruta)).StatusCode);
        }
    }

    [Fact]
    public async Task Pasada_la_hora_sin_resultado_sigue_proximo_y_dice_que_falta_el_resultado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado, new TimeOnly(9, 0));

        _fabrica.Reloj.FijarEnColombia(partido.Fecha, new TimeOnly(11, 0));
        var detalle = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}")));

        Assert.Equal("PROXIMO", detalle.GetProperty("estado").GetString());
        Assert.True(detalle.GetProperty("faltaResultado").GetBoolean());
    }
}
