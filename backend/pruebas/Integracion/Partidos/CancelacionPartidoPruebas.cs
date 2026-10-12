using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// Cambiar, cancelar y reactivar un partido: un cancelado lo siguen viendo solo quienes lo veían, y
/// reactivado vuelve con la misma convocatoria y las mismas respuestas (historia 2, escenarios 2.9 y
/// 2.11; RF-014, RF-038).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CancelacionPartidoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public CancelacionPartidoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Sabado => _fabrica.Reloj.Hoy.AddDays(4);

    [Fact]
    public async Task Cancelado_lo_ven_solo_quienes_lo_veian_y_reactivado_conserva_convocatoria_y_respuestas()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014, e.C2015], Sabado, convocados: [e.Jn(3).Integrante, e.K1.Integrante]);
        await _fabrica.ConContextoAsync(contexto => contexto.PartidosJugador.IgnoreQueryFilters()
            .Where(fila => fila.PartidoId == partido.Id && fila.UsuarioRolId == e.Jn(3).Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Respuesta, RespuestaFamilia.ASISTIRA)));
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        var cancelado = await Actividades.LeerAsync(await e.E2.Cliente.PostAsync($"{ruta}/cancelacion"));
        Assert.Equal("CANCELADO", cancelado.GetProperty("estado").GetString());
        foreach (var persona in new[] { e.Jn(3), e.K1, e.E1, e.E2, e.D1, e.P })
        {
            Assert.Equal("CANCELADO", (await Actividades.LeerAsync(await persona.Cliente.GetAsync(ruta))).GetProperty("estado").GetString());
        }

        foreach (var persona in new[] { e.Jn(4), e.D2, e.M1 })
        {
            await Actividades.ErrorAsync(await persona.Cliente.GetAsync(ruta), HttpStatusCode.NotFound, "no_encontrado");
        }

        var reactivado = await Actividades.LeerAsync(await e.E2.Cliente.DeleteAsync($"{ruta}/cancelacion"));
        Assert.Equal("PROXIMO", reactivado.GetProperty("estado").GetString());
        var planilla = await Actividades.LeerAsync(await e.E2.Cliente.GetAsync($"{ruta}/jugadores"));
        Assert.Equal(2, planilla.GetProperty("convocados").GetArrayLength());
        Assert.Equal(1, planilla.GetProperty("asistiran").GetInt32());
        Assert.Equal("ASISTIRA", (await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(ruta))).GetProperty("miRespuesta").GetString());
        await Actividades.ErrorAsync(await e.Jn(4).Cliente.GetAsync(ruta), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Ya_empezado_no_se_reactiva()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado, new TimeOnly(9, 0), estado: EstadoPartido.CANCELADO);

        _fabrica.Reloj.FijarEnColombia(partido.Fecha, new TimeOnly(9, 30));

        await Actividades.ErrorAsync(
            await e.E1.Cliente.DeleteAsync(e.Ruta($"/partidos/{partido.Id}/cancelacion")), HttpStatusCode.Conflict, "actividad_empezada");
    }

    [Fact]
    public async Task Cambiar_el_rival_lo_ven_los_convocados_y_las_categorias_no_cambian()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado, convocados: [e.Jn(3).Integrante]);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Actividades.Partido(Sabado, [e.C2014], rival: "Equipo D")));

        Assert.Equal("Equipo D", (await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(ruta))).GetProperty("rival").GetString());
        await Actividades.ErrorAsync(
            await e.P.Cliente.PutAsync(ruta, Actividades.Partido(Sabado, [e.C2014, e.C2015])), HttpStatusCode.Conflict, "categorias_no_cambian");
    }

    [Fact]
    public async Task Quien_no_entrena_todas_sus_categorias_no_lo_cancela()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014, e.C2015], Sabado);

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PostAsync(e.Ruta($"/partidos/{partido.Id}/cancelacion")), HttpStatusCode.Forbidden, "no_gestiona_actividad");
    }
}
