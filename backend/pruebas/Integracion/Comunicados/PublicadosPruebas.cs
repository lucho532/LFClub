using System.Net;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Comunicados.EscenarioComunicados;

namespace LFClub.Pruebas.Integracion.Comunicados;

/// <summary>
/// Lo que ve el equipo del club en "Comunicados publicados": el PRESIDENTE y el DIRECTIVO, todo; el
/// ENTRENADOR, solo lo suyo (spec 011, historia 4; RF-024).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PublicadosPruebas
{
    private readonly FabricaApi _fabrica;

    public PublicadosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_entrenador_ve_solo_los_suyos_y_el_presidente_y_el_directivo_todos()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var deE1 = (await e.PublicarAsync(e.E1, Cuerpo("CATEGORIAS", [e.Club.C2014]))).GetProperty("id").GetGuid();
        var deP = (await e.PublicarAsync(e.P, Cuerpo("CLUB"))).GetProperty("id").GetGuid();
        var deD = (await e.PublicarAsync(e.D, Cuerpo("CATEGORIAS", [e.Club.C2015]))).GetProperty("id").GetGuid();

        var vistoPorE1 = await Actividades.LeerAsync(await e.E1.GetAsync(e.Ruta()));
        var vistoPorP = await Actividades.LeerAsync(await e.P.GetAsync(e.Ruta()));
        var vistoPorD = await Actividades.LeerAsync(await e.D.GetAsync(e.Ruta()));

        Assert.Equal([deE1], Actividades.Ids(vistoPorE1.GetProperty("items"), "id"));
        Assert.Equal(new[] { deD, deP, deE1 }.Order(), Actividades.Ids(vistoPorP.GetProperty("items"), "id").Order());
        Assert.Equal(3, Actividades.Ids(vistoPorD.GetProperty("items"), "id").Count);
        Assert.False(vistoPorP.GetProperty("hayMas").GetBoolean());

        var primero = vistoPorP.GetProperty("items").EnumerateArray()
            .Single(comunicado => comunicado.GetProperty("id").GetGuid() == deE1);
        Assert.Equal("ENTRENADOR", primero.GetProperty("publicadoPor").GetProperty("rol").GetString());
        Assert.Equal("2014", primero.GetProperty("destinos")[0].GetProperty("categoriaNombre").GetString());
        Assert.Equal(2, primero.GetProperty("familiasAlcanzadas").GetInt32());
    }

    [Fact]
    public async Task Puede_retirar_solo_quien_lo_publico_o_el_presidente()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        await e.PublicarAsync(e.E1, Cuerpo("CATEGORIAS", [e.Club.C2014]));

        var vistoPorP = await Actividades.LeerAsync(await e.P.GetAsync(e.Ruta()));
        var vistoPorD = await Actividades.LeerAsync(await e.D.GetAsync(e.Ruta()));
        var vistoPorE1 = await Actividades.LeerAsync(await e.E1.GetAsync(e.Ruta()));

        Assert.True(vistoPorP.GetProperty("items")[0].GetProperty("puedeRetirar").GetBoolean());
        Assert.False(vistoPorD.GetProperty("items")[0].GetProperty("puedeRetirar").GetBoolean());
        Assert.True(vistoPorE1.GetProperty("items")[0].GetProperty("puedeRetirar").GetBoolean());
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_recibe_403()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        await Actividades.ErrorAsync(
            await e.Ana.GetAsync(e.Ruta()), HttpStatusCode.Forbidden, "rol_no_autorizado");
    }

    [Fact]
    public async Task El_cursor_pagina_sin_repetir_ni_saltar_con_35_comunicados()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var creados = new List<Guid>();
        for (var i = 0; i < 35; i++)
        {
            var comunicado = await _fabrica.Comunicados.PublicarAsync(
                e.Club.Club,
                e.Club.Presidente.Integrante,
                $"Comunicado {i}",
                "Texto",
                LFClub.Dominio.Enumeraciones.AlcanceComunicado.CLUB,
                destinatarios: [e.Club.Dani.Integrante]);
            creados.Add(comunicado.Id);
        }

        var vistos = new List<Guid>();
        Guid? cursor = null;
        var paginas = 0;
        do
        {
            var ruta = e.Ruta(cursor is null ? string.Empty : $"?antesDe={cursor}");
            var pagina = await Actividades.LeerAsync(await e.P.GetAsync(ruta));
            var ids = Actividades.Ids(pagina.GetProperty("items"), "id");
            vistos.AddRange(ids);
            cursor = pagina.GetProperty("hayMas").GetBoolean() ? ids[^1] : null;
            paginas++;
        }
        while (cursor is not null);

        Assert.Equal(2, paginas);
        Assert.Equal(35, vistos.Count);
        Assert.Equal(creados.Order(), vistos.Order());
        Assert.Equal(vistos.Count, vistos.Distinct().Count());
    }

    [Fact]
    public async Task El_limite_se_acota_a_su_rango()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        await e.PublicarAsync(e.P, Cuerpo("CLUB"));
        await e.PublicarAsync(e.P, Cuerpo("CLUB"));

        var uno = await Actividades.LeerAsync(await e.P.GetAsync(e.Ruta("?limite=1")));

        Assert.Single(uno.GetProperty("items").EnumerateArray());
        Assert.True(uno.GetProperty("hayMas").GetBoolean());
    }

    [Fact]
    public async Task Un_integrante_de_otro_club_recibe_404()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var otro = await EscenarioComunicados.CrearAsync(_fabrica);

        await Actividades.ErrorAsync(await otro.P.GetAsync(e.Ruta()), HttpStatusCode.NotFound, "no_encontrado");
    }
}
