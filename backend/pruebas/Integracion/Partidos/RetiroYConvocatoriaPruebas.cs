using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// El retiro saca al jugador de sus convocatorias pendientes y borra las respuestas futuras de su
/// familia, y conserva su historial: partidos jugados, goles y asistencia (RF-033; quickstart 6.4).
/// Vale para los dos caminos del retiro: el del club y el de la aprobación de un hermano en otro club.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RetiroYConvocatoriaPruebas
{
    private readonly FabricaApi _fabrica;

    public RetiroYConvocatoriaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    private async Task<Historial> SembrarAsync(EscenarioActividades e)
    {
        var j4 = e.Jn(4).Integrante;
        var proximo = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Hoy.AddDays(3), convocados: [j4]);
        var cancelado = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], Hoy.AddDays(4), convocados: [j4], estado: EstadoPartido.CANCELADO);
        var jugado = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Hoy.AddDays(-3), convocados: [j4]);
        await _fabrica.Actividades.RegistrarJugadoAsync(jugado, 2, 0, (j4, 2));
        var pasado = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(-2));
        await _fabrica.Actividades.RegistrarAsistenciaAsync(pasado, j4, true);
        var futuro = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(2));
        await _fabrica.Actividades.ResponderAsync(futuro, j4, RespuestaFamilia.ASISTIRA);
        return new Historial(proximo, cancelado, jugado, futuro);
    }

    private async Task ComprobarAsync(EscenarioActividades e, Historial h)
    {
        var j4 = e.Jn(4).Id;
        var (convocatorias, respuestas, asistencias) = await _fabrica.ConContextoAsync(async contexto => (
            await contexto.PartidosJugador.IgnoreQueryFilters().Where(fila => fila.UsuarioRolId == j4).Select(fila => fila.PartidoId).ToListAsync(),
            await contexto.RespuestasEntrenamiento.IgnoreQueryFilters().CountAsync(fila => fila.UsuarioRolId == j4),
            await contexto.AsistenciasEntrenamiento.IgnoreQueryFilters().CountAsync(fila => fila.UsuarioRolId == j4)));

        Assert.Equal([h.Jugado.Id], convocatorias);
        Assert.Equal(0, respuestas);
        Assert.Equal(1, asistencias);

        var goleadores = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/goleadores?categoriaId={e.C2014.Id}")));
        Assert.Contains(e.Jn(4).Integrante.Apellidos, goleadores.GetRawText());

        // Reactivar el cancelado no lo vuelve a convocar, y la planilla de un entrenamiento futuro ya no lo trae.
        await Actividades.LeerAsync(await e.P.Cliente.DeleteAsync(e.Ruta($"/partidos/{h.Cancelado.Id}/cancelacion")));
        var planilla = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/partidos/{h.Cancelado.Id}/jugadores")));
        Assert.DoesNotContain(j4, Actividades.Ids(planilla.GetProperty("convocados"), "usuarioRolId"));
        var deEntrenamiento = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/entrenamientos/{h.Futuro.Id}/jugadores")));
        Assert.DoesNotContain(j4, Actividades.Ids(deEntrenamiento.GetProperty("jugadores"), "usuarioRolId"));
    }

    [Fact]
    public async Task Al_retirarlo_el_club_sale_de_lo_pendiente_y_conserva_su_historial()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var historial = await SembrarAsync(e);

        var respuesta = await e.P.Cliente.PostAsync(e.Ruta($"/jugadores/{e.Jn(4).Id}/retiro"));

        Assert.True(respuesta.IsSuccessStatusCode, $"{(int)respuesta.StatusCode}");
        await ComprobarAsync(e, historial);
    }

    [Fact]
    public async Task Al_aprobarse_un_hermano_con_su_documento_en_otro_club_pasa_lo_mismo_y_el_otro_club_no_cambia()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var historial = await SembrarAsync(e);
        var deB = await _fabrica.Actividades.CrearPartidoAsync([e.CB], Hoy.AddDays(3), convocados: [e.JugadorB.Integrante]);
        var hermano = await _fabrica.Hermanos.CrearHermanoAsync(
            e.JugadorB.Integrante, categoria: e.CB, numeroDocumento: e.Jn(4).Integrante.NumeroDocumento);

        var aprobacion = await e.PB.Cliente.PostAsync(e.RutaB($"/ingresos/{hermano.Id}/aprobacion"), new { });

        Assert.True(aprobacion.IsSuccessStatusCode, $"{(int)aprobacion.StatusCode}: {await aprobacion.Content.ReadAsStringAsync()}");
        await ComprobarAsync(e, historial);
        Assert.True(await _fabrica.ConContextoAsync(contexto => contexto.PartidosJugador.IgnoreQueryFilters()
            .AnyAsync(fila => fila.PartidoId == deB.Id && fila.UsuarioRolId == e.JugadorB.Id)));
        Assert.Equal(HttpStatusCode.OK, (await e.JugadorB.Cliente.ElegirJugador(e.JugadorB.Id).GetAsync(e.RutaB($"/partidos/{deB.Id}"))).StatusCode);
    }

    private sealed record Historial(Partido Proximo, Partido Cancelado, Partido Jugado, Entrenamiento Futuro);
}
