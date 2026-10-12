using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LaPecosa.Pruebas.Integracion.Comunicados.EscenarioComunicados;

namespace LaPecosa.Pruebas.Integracion.Comunicados;

/// <summary>
/// Retirar un comunicado: quién puede, qué pasa en la campana y en lo publicado (spec 011,
/// historia 4; RF-025 a RF-027).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RetiroPruebas
{
    private readonly FabricaApi _fabrica;

    public RetiroPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Retiro(EscenarioComunicados e, Guid id) => e.Ruta($"/{id}/retiro");

    [Fact]
    public async Task El_autor_lo_retira_y_la_familia_deja_de_verlo_con_el_contador_bajando()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var id = (await e.PublicarAsync(e.E1, Cuerpo("CATEGORIAS", [e.Club.C2014]))).GetProperty("id").GetGuid();
        Assert.Equal(1, await e.SinLeerAsync(e.Ana));

        var retirado = await Actividades.LeerAsync(await e.E1.PostAsync(Retiro(e, id)));

        Assert.False(retirado.GetProperty("puedeRetirar").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(retirado.GetProperty("retirado").GetProperty("porNombre").GetString()));
        Assert.Equal(0, await e.SinLeerAsync(e.Ana));
        Assert.Empty(await e.RecibidosAsync(e.Ana));
    }

    [Fact]
    public async Task Lo_retirado_sigue_en_lo_publicado_con_quien_y_cuando()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var id = (await e.PublicarAsync(e.D, Cuerpo("CLUB"))).GetProperty("id").GetGuid();
        await Actividades.LeerAsync(await e.D.PostAsync(Retiro(e, id)));

        var lista = await Actividades.LeerAsync(await e.P.GetAsync(e.Ruta()));

        var fila = lista.GetProperty("items")[0];
        Assert.Equal(id, fila.GetProperty("id").GetGuid());
        Assert.Equal(
            e.Club.Directivo.Integrante.Nombres + " " + e.Club.Directivo.Integrante.Apellidos,
            fila.GetProperty("retirado").GetProperty("porNombre").GetString());
        Assert.True(fila.GetProperty("retirado").TryGetProperty("en", out _));
        Assert.False(fila.GetProperty("puedeRetirar").GetBoolean());
    }

    [Fact]
    public async Task El_presidente_retira_el_de_cualquiera_y_el_entrenador_o_el_directivo_ajenos_no()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var deP = (await e.PublicarAsync(e.P, Cuerpo("CLUB"))).GetProperty("id").GetGuid();
        var deE1 = (await e.PublicarAsync(e.E1, Cuerpo("CATEGORIAS", [e.Club.C2014]))).GetProperty("id").GetGuid();

        // E1 sobre uno del PRESIDENTE: no lo ve (404). D1 sobre uno de otro: lo ve y no lo retira (403).
        await Actividades.ErrorAsync(await e.E1.PostAsync(Retiro(e, deP)), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.D.PostAsync(Retiro(e, deP)), HttpStatusCode.Forbidden, "no_retira_comunicado");
        await Actividades.ErrorAsync(
            await e.D.PostAsync(Retiro(e, deE1)), HttpStatusCode.Forbidden, "no_retira_comunicado");
        // El PRESIDENTE retira el de E1.
        await Actividades.LeerAsync(await e.P.PostAsync(Retiro(e, deE1)));
        // E2 no ve ninguno y los jugadores no retiran.
        await Actividades.ErrorAsync(await e.E2.PostAsync(Retiro(e, deP)), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.Ana.PostAsync(Retiro(e, deP)), HttpStatusCode.Forbidden, "rol_no_autorizado");
    }

    [Fact]
    public async Task Retirar_otra_vez_responde_409_y_no_vuelve_a_publicarse_con_la_misma_clave()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var clave = Guid.NewGuid();
        var id = (await e.PublicarAsync(e.P, Cuerpo("CLUB", clave: clave))).GetProperty("id").GetGuid();
        await Actividades.LeerAsync(await e.P.PostAsync(Retiro(e, id)));

        await Actividades.ErrorAsync(await e.P.PostAsync(Retiro(e, id)), HttpStatusCode.Conflict, "comunicado_retirado");

        var otraVez = await Actividades.LeerAsync(await e.P.PostAsync(e.Ruta(), Cuerpo("CLUB", clave: clave)));
        Assert.Equal(id, otraVez.GetProperty("id").GetGuid());
        Assert.True(otraVez.TryGetProperty("retirado", out _));
        Assert.Empty(await e.RecibidosAsync(e.Ana));
    }

    [Fact]
    public async Task Un_entrenador_que_ya_no_entrena_la_categoria_conserva_y_retira_lo_suyo_pero_no_publica_a_ella()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var id = (await e.PublicarAsync(e.E1, Cuerpo("CATEGORIAS", [e.Club.C2014]))).GetProperty("id").GetGuid();
        await _fabrica.ConContextoAsync(async contexto =>
        {
            var asignacion = await contexto.AsignacionesEntrenadorCategoria.IgnoreQueryFilters()
                .FirstAsync(fila => fila.Id == e.Club.AsignacionE1.Id);
            asignacion.Activa = false;
            return await contexto.SaveChangesAsync();
        });

        await Actividades.ErrorAsync(
            await e.E1.PostAsync(e.Ruta(), Cuerpo("CATEGORIAS", [e.Club.C2014])),
            HttpStatusCode.Forbidden,
            "no_entrena_categoria");
        var lista = await Actividades.LeerAsync(await e.E1.GetAsync(e.Ruta()));
        Assert.Equal([id], Actividades.Ids(lista.GetProperty("items"), "id"));
        await Actividades.LeerAsync(await e.E1.PostAsync(Retiro(e, id)));
    }

    [Fact]
    public async Task Un_comunicado_sin_autor_solo_lo_retira_el_presidente()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var comunicado = await _fabrica.Comunicados.PublicarAsync(
            e.Club.Club, e.Club.E1.Integrante, "Huérfano", "Texto", AlcanceComunicado.CLUB,
            destinatarios: [e.AnaJugador]);
        await _fabrica.ConContextoAsync(contexto => contexto.Comunicados.IgnoreQueryFilters()
            .Where(fila => fila.Id == comunicado.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.PublicadoPorUsuarioRolId, (Guid?)null)));

        await Actividades.ErrorAsync(
            await e.D.PostAsync(Retiro(e, comunicado.Id)), HttpStatusCode.Forbidden, "no_retira_comunicado");
        await Actividades.ErrorAsync(
            await e.E1.PostAsync(Retiro(e, comunicado.Id)), HttpStatusCode.NotFound, "no_encontrado");
        var retirado = await Actividades.LeerAsync(await e.P.PostAsync(Retiro(e, comunicado.Id)));
        Assert.Equal("Huérfano", retirado.GetProperty("titulo").GetString());
        Assert.Equal("ENTRENADOR", retirado.GetProperty("publicadoPor").GetProperty("rol").GetString());
    }

    [Fact]
    public async Task Un_comunicado_de_otro_club_o_inexistente_responde_404()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var otro = await EscenarioComunicados.CrearAsync(_fabrica);
        var id = (await e.PublicarAsync(e.P, Cuerpo("CLUB"))).GetProperty("id").GetGuid();

        await Actividades.ErrorAsync(
            await otro.P.PostAsync(Retiro(e, id)), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await otro.P.PostAsync(otro.Ruta($"/{id}/retiro")), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.P.PostAsync(Retiro(e, Guid.NewGuid())), HttpStatusCode.NotFound, "no_encontrado");
    }
}
