using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Avisos;

/// <summary>
/// Cambiar, cancelar y reactivar un partido avisa a las familias que lo ven: un partido próximo,
/// solo a las de sus convocados; crearlo no avisa a nadie (spec 012, historia 2; RF-002, RF-003,
/// RF-004, RF-007, RF-017 y CE-002).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisosDePartidoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public AvisosDePartidoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    [Fact]
    public async Task Crear_un_partido_no_avisa_a_nadie_aunque_haya_jugadores_en_la_categoria()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var creado = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/partidos"), Actividades.Partido(Manana, [e.C2014])),
            HttpStatusCode.Created);

        Assert.Empty(await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, creado.GetProperty("partidoId").GetGuid()));
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
    }

    [Fact]
    public async Task Cambiar_el_lugar_o_la_hora_avisa_solo_a_los_convocados_y_el_resto_ni_ve_el_partido()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], Manana, convocados: [e.Jn(3).Integrante, e.Jn(4).Integrante]);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        var cuerpo = Actividades.Partido(Manana, [e.C2014], hora: "11:00");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, cuerpo));

        foreach (var convocado in new[] { e.Jn(3), e.Jn(4) })
        {
            var textos = await AyudaDeAvisos.TextosAsync(e, convocado);
            Assert.Equal(["Cambió la hora del partido contra Equipo C: ahora mañana a las 11:00"], textos);
        }

        // Una familia no convocada no ve ningún aviso del partido y el detalle le responde 404 (CE-002).
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(5)));
        Assert.Equal(HttpStatusCode.NotFound, (await e.Jn(5).Cliente.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task Cancelar_y_reactivar_avisan_a_los_convocados_y_el_aviso_abre_el_partido_cancelado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Manana, convocados: [e.Jn(3).Integrante]);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"));
        var cancelado = (await AyudaDeAvisos.AvisosAsync(e, e.Jn(3))).Single();
        Assert.Equal("Partido contra Equipo C de mañana cancelado", cancelado.GetProperty("texto").GetString());
        Assert.True(cancelado.GetProperty("actividad").GetProperty("disponible").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await e.Jn(3).Cliente.GetAsync(ruta)).StatusCode);

        await Actividades.LeerAsync(await e.E1.Cliente.DeleteAsync($"{ruta}/cancelacion"));
        Assert.Equal(
            ["El partido contra Equipo C de mañana sigue en pie"], await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(5)));
    }

    [Fact]
    public async Task Cambiar_el_rival_la_condicion_el_torneo_o_guardar_lo_mismo_no_avisa()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Manana, convocados: [e.Jn(3).Integrante]);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Actividades.Partido(Manana, [e.C2014])));
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            ruta, Actividades.Partido(Manana, [e.C2014], rival: "Otro Equipo", condicion: "VISITANTE")));
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            ruta, Actividades.Partido(Manana, [e.C2014], rival: "Otro Equipo", condicion: "VISITANTE", tipo: "TORNEO", nombreTorneo: "Copa")));

        Assert.Empty(await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, partido.Id));
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
    }
}
