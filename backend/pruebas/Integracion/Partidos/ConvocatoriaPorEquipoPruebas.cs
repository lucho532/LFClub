using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// La planilla del partido trae los equipos de sus categorías y los equipos de cada jugador para
/// convocar por equipo; un jugador que llega por dos equipos queda convocado una sola vez y la
/// visibilidad del partido no cambia (spec 013, historia 2; RF-009 a RF-012).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConvocatoriaPorEquipoPruebas
{
    private readonly FabricaApi _fabrica;

    public ConvocatoriaPorEquipoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Sabado => _fabrica.Reloj.Hoy.AddDays(3);

    private static object Convocar(params Guid[] ids) => new { usuarioRolIds = ids };

    private static JsonElement Fila(JsonElement lista, Guid usuarioRolId) =>
        lista.EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == usuarioRolId);

    [Fact]
    public async Task La_planilla_trae_los_equipos_y_los_equipos_de_cada_jugador()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado, convocados: [e.Jn(3).Integrante]);

        var planilla = await Actividades.LeerAsync(
            await e.E1.Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}/jugadores")));

        var equipos = planilla.GetProperty("equipos").EnumerateArray().Select(equipo => equipo.GetProperty("nombre").GetString()).ToList();
        Assert.Equal(["A", "B"], equipos);
        var deJ6 = Fila(planilla.GetProperty("convocables"), e.Jn(6).Id);
        Assert.Equivalent(
            new[] { a.Id, b.Id },
            deJ6.GetProperty("equipoIds").EnumerateArray().Select(id => id.GetGuid()).ToArray());
        Assert.Empty(Fila(planilla.GetProperty("convocables"), e.Jn(7).Id).GetProperty("equipoIds").EnumerateArray());
        var deJ3 = Fila(planilla.GetProperty("convocados"), e.Jn(3).Id);
        Assert.Equal(a.Id, Assert.Single(deJ3.GetProperty("equipoIds").EnumerateArray()).GetGuid());
        Assert.False(deJ3.GetProperty("anadido").GetBoolean());
    }

    [Fact]
    public async Task Un_jugador_repetido_por_dos_caminos_queda_convocado_una_sola_vez()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Actividades.CrearEquiposAsync(e);
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado);

        var guardada = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            e.Ruta($"/partidos/{partido.Id}/convocatoria"), Convocar(e.Jn(6).Id, e.Jn(6).Id, e.Jn(3).Id)));

        Assert.Equal(2, guardada.GetProperty("convocados").GetArrayLength());
        var filas = await _fabrica.ConContextoAsync(contexto => contexto.PartidosJugador.IgnoreQueryFilters()
            .CountAsync(fila => fila.PartidoId == partido.Id && fila.UsuarioRolId == e.Jn(6).Id));
        Assert.Equal(1, filas);
    }

    [Fact]
    public async Task Solo_los_convocados_ven_el_partido_proximo_aunque_sean_del_mismo_equipo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Actividades.CrearEquiposAsync(e);
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado);
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            e.Ruta($"/partidos/{partido.Id}/convocatoria"), Convocar(e.Jn(3).Id)));

        // J4 es del mismo equipo que J3 y no está convocado: para él el partido no existe.
        Assert.Equal(HttpStatusCode.OK, (await e.Jn(3).Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Jn(4).Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}"))).StatusCode);
    }

    [Fact]
    public async Task Marcar_a_todos_los_de_un_equipo_y_quitar_a_uno_deja_a_los_demas()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Actividades.CrearEquiposAsync(e);
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Sabado);
        var ruta = e.Ruta($"/partidos/{partido.Id}/convocatoria");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3).Id, e.Jn(4).Id, e.Jn(6).Id)));
        var tras = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3).Id, e.Jn(6).Id, e.Jn(5).Id)));

        var ids = Actividades.Ids(tras.GetProperty("convocados"), "usuarioRolId");
        Assert.Equivalent(new[] { e.Jn(3).Id, e.Jn(6).Id, e.Jn(5).Id }, ids);
    }
}
