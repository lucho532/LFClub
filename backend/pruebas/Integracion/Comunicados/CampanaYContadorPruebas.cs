using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using static LaPecosa.Pruebas.Integracion.Comunicados.EscenarioComunicados;

namespace LaPecosa.Pruebas.Integracion.Comunicados;

/// <summary>
/// La campana: el contador y la lista de recibidos de la cuenta de un jugador (spec 011, historia 2;
/// RF-012 a RF-019 y RF-032).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CampanaYContadorPruebas
{
    private readonly FabricaApi _fabrica;

    public CampanaYContadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_contador_es_igual_a_los_sin_leer_y_la_lista_va_del_mas_reciente_al_mas_antiguo()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var club = e.Club;
        var uno = await _fabrica.Comunicados.PublicarAsync(
            club.Club, club.Presidente.Integrante, "Uno", "Texto uno", AlcanceComunicado.CLUB,
            destinatarios: [club.Ana.Integrante]);
        var dos = await _fabrica.Comunicados.PublicarAsync(
            club.Club, club.E1.Integrante, "Dos", "Texto dos", AlcanceComunicado.CATEGORIAS,
            [club.C2014], destinatarios: [club.Ana.Integrante]);
        var tres = await _fabrica.Comunicados.PublicarAsync(
            club.Club, club.Directivo.Integrante, "Tres", "Texto tres", AlcanceComunicado.CLUB,
            destinatarios: [club.Ana.Integrante]);
        await _fabrica.Comunicados.LeerAsync(uno, club.Ana.Usuario);

        var pagina = await Actividades.LeerAsync(await e.Ana.GetAsync(e.RutaCampana()));

        Assert.Equal(2, await e.SinLeerAsync(e.Ana));
        Assert.Equal(2, pagina.GetProperty("sinLeer").GetInt32());
        Assert.Equal(
            new[] { tres.Id, dos.Id, uno.Id }.OrderByDescending(id => id),
            EscenarioComunicados.ComunicadosDe(pagina));
        var recibidos = pagina.GetProperty("elementos").EnumerateArray()
            .Select(elemento => elemento.GetProperty("comunicado"))
            .ToDictionary(comunicado => comunicado.GetProperty("titulo").GetString()!);
        Assert.False(recibidos["Tres"].GetProperty("leido").GetBoolean());
        Assert.True(recibidos["Uno"].GetProperty("leido").GetBoolean());
        Assert.Equal("ENTRENADOR", recibidos["Dos"].GetProperty("publicadoPor").GetProperty("rol").GetString());
        Assert.True(recibidos["Uno"].TryGetProperty("leidoEn", out _));
        Assert.False(recibidos["Tres"].TryGetProperty("leidoEn", out _));
    }

    [Fact]
    public async Task Una_cuenta_sin_comunicados_tiene_contador_en_cero_y_lista_vacia()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        var pagina = await Actividades.LeerAsync(await e.Dani.GetAsync(e.RutaCampana()));

        Assert.Equal(0, await e.SinLeerAsync(e.Dani));
        Assert.Empty(pagina.GetProperty("elementos").EnumerateArray());
        Assert.False(pagina.GetProperty("hayMas").GetBoolean());
    }

    [Fact]
    public async Task Los_hermanos_cuentan_uno_solo_aunque_el_comunicado_les_llegue_a_los_dos()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            e.AnaJugador, EstadoIngreso.APROBADO, categoria: e.Club.C2015, nombres: "Hugo");
        await e.PublicarAsync(e.P, Cuerpo("CATEGORIAS", [e.Club.C2014, e.Club.C2015]));

        Assert.Equal(1, await e.SinLeerAsync(e.Ana));
        Assert.Single(await e.RecibidosAsync(e.Ana));
        Assert.NotEqual(e.AnaJugador.Id, hugo.Id);
    }

    [Fact]
    public async Task Solo_la_cuenta_de_un_jugador_usa_la_campana()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        foreach (var cliente in new[] { e.P, e.D, e.E1 })
        {
            await Actividades.ErrorAsync(
                await cliente.GetAsync(e.RutaCampana()), HttpStatusCode.Forbidden, "rol_no_autorizado");
            await Actividades.ErrorAsync(
                await cliente.GetAsync(e.RutaCampana("/contador")), HttpStatusCode.Forbidden, "rol_no_autorizado");
        }
    }

    [Fact]
    public async Task Cambiar_de_categoria_conserva_lo_recibido_y_no_trae_lo_anterior()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var antes = (await e.PublicarAsync(e.P, Cuerpo("CATEGORIAS", [e.Club.C2014]))).GetProperty("id").GetGuid();
        var aLa2015Antes = (await e.PublicarAsync(e.P, Cuerpo("CATEGORIAS", [e.Club.C2015]))).GetProperty("id").GetGuid();

        await _fabrica.Comunicados.MoverACategoriaAsync(e.Club.Beto.Integrante, e.Club.C2015);
        var despues = (await e.PublicarAsync(e.P, Cuerpo("CATEGORIAS", [e.Club.C2015]))).GetProperty("id").GetGuid();

        var deBeto = await e.RecibidosAsync(e.Beto);
        Assert.Contains(antes, deBeto);
        Assert.Contains(despues, deBeto);
        Assert.DoesNotContain(aLa2015Antes, deBeto);
    }

    [Fact]
    public async Task Quien_entra_despues_no_recibe_lo_anterior_y_el_en_espera_nunca_recibe()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var nuevo = await _fabrica.Categorias.CrearJugadorAsync(e.Club.Club, 2014, e.Club.C2014, "Nico");
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(nuevo.Usuario);
        var enEspera = await _fabrica.Categorias.CrearJugadorEnEsperaAsync(e.Club.Club, 2014);
        var clienteEnEspera = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(enEspera.Usuario);

        var publicado = await e.PublicarAsync(e.P, Cuerpo("CLUB"));
        var llegaDespues = await _fabrica.Categorias.CrearJugadorAsync(e.Club.Club, 2014, e.Club.C2014, "Omar");
        var clienteDespues = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(llegaDespues.Usuario);

        Assert.Contains(publicado.GetProperty("id").GetGuid(), await e.RecibidosAsync(cliente));
        Assert.Empty(await e.RecibidosAsync(clienteDespues));
        // La sala de espera no abre nada del club.
        await Actividades.ErrorAsync(
            await clienteEnEspera.GetAsync(e.RutaCampana()), HttpStatusCode.Forbidden, "ingreso_en_espera");
    }

    [Fact]
    public async Task Un_jugador_retirado_deja_de_contar_y_al_reincorporarlo_vuelve()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var publicado = await e.PublicarAsync(e.P, Cuerpo("CATEGORIAS", [e.Club.C2014]));
        var id = publicado.GetProperty("id").GetGuid();
        Assert.Equal(1, await e.SinLeerAsync(e.Beto));

        await _fabrica.Categorias.RetirarAsync(e.Club.Beto.Integrante, e.Club.Presidente.Integrante);
        await Actividades.ErrorAsync(
            await e.Beto.GetAsync(e.RutaCampana("/contador")), HttpStatusCode.Forbidden, "integrante_retirado");

        await _fabrica.Comunicados.ReincorporarAsync(e.Club.Beto.Integrante, e.Club.C2014);
        Assert.Equal(1, await e.SinLeerAsync(e.Beto));
        Assert.Contains(id, await e.RecibidosAsync(e.Beto));
    }

    [Fact]
    public async Task Un_hermano_retirado_deja_de_contar_aunque_la_cuenta_siga_activa()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            e.AnaJugador, EstadoIngreso.APROBADO, categoria: e.Club.C2015, nombres: "Hugo");
        var soloDeHugo = await _fabrica.Comunicados.PublicarAsync(
            e.Club.Club, e.Club.Presidente.Integrante, "Para Hugo", "Texto", AlcanceComunicado.CATEGORIAS,
            [e.Club.C2015], destinatarios: [hugo]);
        Assert.Equal(1, await e.SinLeerAsync(e.Ana));

        await _fabrica.Categorias.RetirarAsync(hugo, e.Club.Presidente.Integrante);

        Assert.Equal(0, await e.SinLeerAsync(e.Ana));
        Assert.DoesNotContain(soloDeHugo.Id, await e.RecibidosAsync(e.Ana));
    }

    [Fact]
    public async Task Una_sesion_limitada_a_un_jugador_solo_ve_lo_de_ese_jugador()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            e.AnaJugador, EstadoIngreso.APROBADO, categoria: e.Club.C2015, nombres: "Hugo");
        var deAna = await _fabrica.Comunicados.PublicarAsync(
            e.Club.Club, e.Club.Presidente.Integrante, "De Ana", "Texto", AlcanceComunicado.CATEGORIAS,
            [e.Club.C2014], destinatarios: [e.AnaJugador]);
        var deHugo = await _fabrica.Comunicados.PublicarAsync(
            e.Club.Club, e.Club.Presidente.Integrante, "De Hugo", "Texto", AlcanceComunicado.CATEGORIAS,
            [e.Club.C2015], destinatarios: [hugo]);
        var limitada = await _fabrica.CrearClienteDePrueba()
            .ConSesionLimitadaAsync(e.Club.Ana.Usuario, e.AnaJugador.Id);

        Assert.Equal([deAna.Id], await e.RecibidosAsync(limitada));
        Assert.Equal(1, await e.SinLeerAsync(limitada));
        // La cuenta completa ve los dos.
        Assert.Equal(new[] { deAna.Id, deHugo.Id }.Order(), (await e.RecibidosAsync(e.Ana)).Order());
        // Y la sesión limitada no abre lo del otro hermano.
        await Actividades.ErrorAsync(
            await limitada.GetAsync(e.Ruta($"/recibidos/{deHugo.Id}")), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Lo_retirado_sale_de_la_lista_y_del_contador()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var comunicado = await _fabrica.Comunicados.PublicarAsync(
            e.Club.Club, e.Club.Presidente.Integrante, "Se retira", "Texto", AlcanceComunicado.CLUB,
            destinatarios: [e.AnaJugador]);
        Assert.Equal(1, await e.SinLeerAsync(e.Ana));

        await _fabrica.Comunicados.RetirarAsync(comunicado, e.Club.Presidente.Integrante);

        Assert.Equal(0, await e.SinLeerAsync(e.Ana));
        Assert.Empty(await e.RecibidosAsync(e.Ana));
        await Actividades.ErrorAsync(
            await e.Ana.GetAsync(e.Ruta($"/recibidos/{comunicado.Id}")), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Un_comunicado_ajeno_o_de_otro_club_responde_404_sin_distinguir()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var otro = await EscenarioComunicados.CrearAsync(_fabrica);
        var deCaro = await _fabrica.Comunicados.PublicarAsync(
            e.Club.Club, e.Club.Presidente.Integrante, "Solo Caro", "Texto", AlcanceComunicado.CATEGORIAS,
            [e.Club.C2015], destinatarios: [e.Club.Caro.Integrante]);

        // Otra cuenta del mismo club, un jugador de otro club y un identificador inventado.
        await Actividades.ErrorAsync(
            await e.Ana.GetAsync(e.Ruta($"/recibidos/{deCaro.Id}")), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await otro.Ana.GetAsync(e.Ruta($"/recibidos/{deCaro.Id}")), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.Ana.GetAsync(e.Ruta($"/recibidos/{Guid.NewGuid()}")), HttpStatusCode.NotFound, "no_encontrado");
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await Actividades.ErrorAsync(
            await desarrollador.GetAsync(e.RutaCampana()), HttpStatusCode.NotFound, "no_encontrado");
    }
}
