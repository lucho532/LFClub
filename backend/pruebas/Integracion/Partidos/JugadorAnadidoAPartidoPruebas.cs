using System.Net;
using LFClub.Pruebas.Integracion.Avisos;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// Convocar a un jugador de otra categoría a un partido: lo ve y responde, su categoría y sus
/// equipos no cambian, publica el resultado de su hijo, sus goles cuentan en sus estadísticas con su
/// propia categoría y sacarlo no avisa (spec 013, historia 3; RF-013 a RF-019).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class JugadorAnadidoAPartidoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public JugadorAnadidoAPartidoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private static object Convocar(params Persona[] jugadores) =>
        new { usuarioRolIds = jugadores.Select(jugador => jugador.Id) };

    [Fact]
    public async Task El_anadido_de_otra_categoria_ve_responde_y_no_cambia_de_categoria()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], _fabrica.Reloj.Hoy.AddDays(3));
        var ruta = e.Ruta($"/partidos/{partido.Id}");
        var categoria = e.K1.Integrante.CategoriaId;

        var planilla = await Actividades.LeerAsync(
            await e.E1.Cliente.PutAsync($"{ruta}/convocatoria", Convocar(e.Jn(3), e.K1)));

        var deK1 = planilla.GetProperty("convocados").EnumerateArray()
            .Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == e.K1.Id);
        Assert.True(deK1.GetProperty("anadido").GetBoolean());
        Assert.Equal(2015, deK1.GetProperty("categoria").GetProperty("anio").GetInt32());
        await Actividades.LeerAsync(await e.K1.Cliente.GetAsync(ruta));
        await Actividades.LeerAsync(await e.K1.Cliente.PutAsync($"{ruta}/mi-respuesta", new { respuesta = "ASISTIRA" }));
        var actual = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(integrante => integrante.Id == e.K1.Id).Select(integrante => integrante.CategoriaId).SingleAsync());
        Assert.Equal(categoria, actual);
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.K1));
    }

    [Fact]
    public async Task Sus_goles_cuentan_con_su_propia_categoria_y_sacarlo_no_avisa()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], _fabrica.Reloj.Hoy.AddDays(3), convocados: [e.Jn(3).Integrante, e.K1.Integrante]);
        await _fabrica.Actividades.RegistrarJugadoAsync(partido, 3, 1, (e.K1.Integrante, 2), (e.Jn(3).Integrante, 1));

        var cifras = await Actividades.LeerAsync(
            await e.P.Cliente.GetAsync(e.Ruta($"/jugadores/{e.K1.Id}/estadisticas")));

        Assert.Equal(2, cifras.GetProperty("total").GetProperty("goles").GetInt32());
        Assert.Equal(1, cifras.GetProperty("total").GetProperty("partidosJugados").GetInt32());
    }

    [Fact]
    public async Task Sacar_al_anadido_de_un_partido_proximo_no_le_avisa()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], _fabrica.Reloj.Hoy.AddDays(3));
        var ruta = e.Ruta($"/partidos/{partido.Id}/convocatoria");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3), e.K1)));
        var antes = (await AyudaDeAvisos.TextosAsync(e, e.K1)).Count;

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3))));

        Assert.Equal(antes, (await AyudaDeAvisos.TextosAsync(e, e.K1)).Count);
        Assert.Equal(
            HttpStatusCode.NotFound, (await e.K1.Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}"))).StatusCode);
    }

    [Fact]
    public async Task El_anadido_de_un_partido_jugado_recibe_el_aviso_del_resultado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], _fabrica.Reloj.Hoy.AddDays(3), convocados: [e.Jn(3).Integrante, e.K1.Integrante]);
        await _fabrica.Actividades.RegistrarJugadoAsync(partido, 2, 0, (e.K1.Integrante, 1));
        var rutaPartido = e.Ruta($"/partidos/{partido.Id}");

        // Corregir el marcador de un partido jugado vuelve a publicar el resultado a sus destinatarios.
        await Actividades.LeerAsync(await e.P.Cliente.PutAsync(
            $"{rutaPartido}/resultado",
            new
            {
                golesClub = 3,
                golesRival = 0,
                jugadores = new[] { new { usuarioRolId = e.K1.Id, jugo = true, goles = 1 } },
            }));

        var textos = await AyudaDeAvisos.TextosAsync(e, e.K1);
        Assert.NotEmpty(textos);
    }
}
