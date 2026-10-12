using System.Net;
using System.Text.Json;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Comunicados;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// La campana unificada: comunicados y avisos mezclados del más reciente al más antiguo, un solo
/// contador, "marcar todos" y la lectura por cuenta (spec 012, historia 3; RF-018 a RF-024).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CampanaMezclaPruebas
{
    private readonly FabricaApi _fabrica;

    public CampanaMezclaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<AvisoDeActividad> AvisarAsync(
        EscenarioComunicados e, string texto, DateTimeOffset cuando, params UsuarioRol[] destinatarios) =>
        _fabrica.Avisos.GenerarAsync(
            e.Club.Club, HechoDeActividad.ENTRENAMIENTO_PROGRAMADO, TipoDeActividad.ENTRENAMIENTO, Guid.NewGuid(),
            texto, destinatarios, cuando);

    private Task<Comunicado> ComunicarAsync(EscenarioComunicados e, string titulo, params UsuarioRol[] destinatarios) =>
        _fabrica.Comunicados.PublicarAsync(
            e.Club.Club, e.Club.Presidente.Integrante, titulo, "Texto", AlcanceComunicado.CLUB,
            destinatarios: destinatarios);

    private static List<(string Tipo, Guid Id)> Elementos(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray()
            .Select(elemento =>
            {
                var tipo = elemento.GetProperty("tipo").GetString()!;
                var contenido = elemento.GetProperty(tipo == "AVISO" ? "aviso" : "comunicado");
                return (tipo, contenido.GetProperty("id").GetGuid());
            })
            .ToList();

    [Fact]
    public async Task El_contador_suma_comunicados_y_avisos_y_la_lista_los_mezcla_por_fecha()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var ahora = DateTimeOffset.UtcNow;
        var antiguo = await AvisarAsync(e, "Antiguo", ahora.AddHours(-1), e.AnaJugador);
        var comunicado = await ComunicarAsync(e, "Medio", e.AnaJugador);
        var reciente = await AvisarAsync(e, "Reciente", ahora.AddHours(1), e.AnaJugador);

        var pagina = await Actividades.LeerAsync(await e.Ana.GetAsync(e.RutaCampana()));
        var contador = await Actividades.LeerAsync(await e.Ana.GetAsync(e.RutaCampana("/contador")));

        Assert.Equal(3, contador.GetProperty("sinLeer").GetInt32());
        Assert.Equal(3, pagina.GetProperty("sinLeer").GetInt32());
        Assert.Equal(
            [("AVISO", reciente.Id), ("COMUNICADO", comunicado.Id), ("AVISO", antiguo.Id)],
            Elementos(pagina));
    }

    [Fact]
    public async Task Un_elemento_trae_solo_el_contenido_de_su_tipo_y_el_aviso_no_lleva_autor_ni_titulo()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        await AvisarAsync(e, "Nuevo entrenamiento el sábado a las 9:00", DateTimeOffset.UtcNow, e.AnaJugador);
        await ComunicarAsync(e, "Un título", e.AnaJugador);

        var pagina = await Actividades.LeerAsync(await e.Ana.GetAsync(e.RutaCampana()));

        foreach (var elemento in pagina.GetProperty("elementos").EnumerateArray())
        {
            var esAviso = elemento.GetProperty("tipo").GetString() == "AVISO";
            Assert.Equal(esAviso, elemento.TryGetProperty("aviso", out var aviso));
            Assert.Equal(!esAviso, elemento.TryGetProperty("comunicado", out _));
            if (esAviso)
            {
                Assert.False(aviso.TryGetProperty("titulo", out _));
                Assert.False(aviso.TryGetProperty("publicadoPor", out _));
                Assert.False(aviso.TryGetProperty("leidoEn", out _));
                Assert.True(aviso.GetProperty("texto").GetString()!.Length <= 300);
                Assert.True(aviso.GetProperty("actividad").TryGetProperty("disponible", out _));
            }
        }
    }

    [Fact]
    public async Task Un_aviso_que_llega_por_dos_hermanos_cuenta_y_sale_una_sola_vez()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            e.AnaJugador, EstadoIngreso.APROBADO, categoria: e.Club.C2014, nombres: "Hugo");
        var aviso = await AvisarAsync(e, "Para los dos", DateTimeOffset.UtcNow, e.AnaJugador, hugo);
        var comunicado = await ComunicarAsync(e, "Para los dos", e.AnaJugador, hugo);

        var pagina = await Actividades.LeerAsync(await e.Ana.GetAsync(e.RutaCampana()));

        Assert.Equal(2, pagina.GetProperty("sinLeer").GetInt32());
        Assert.Equal(
            new[] { aviso.Id, comunicado.Id }.Order().ToList(),
            Elementos(pagina).Select(elemento => elemento.Id).Order().ToList());
    }

    [Fact]
    public async Task El_cursor_recorre_la_mezcla_sin_repetir_ni_saltar_y_hayMas_acompana_cada_pagina()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var ahora = DateTimeOffset.UtcNow;
        var esperados = new List<Guid>();
        for (var i = 0; i < 7; i++)
        {
            esperados.Add((await AvisarAsync(e, $"Aviso {i}", ahora.AddMinutes(i - 20), e.AnaJugador)).Id);
            esperados.Add((await ComunicarAsync(e, $"Comunicado {i}", e.AnaJugador)).Id);
        }

        var vistos = new List<Guid>();
        Guid? cursor = null;
        var paginas = 0;
        do
        {
            var resto = $"?limite=4{(cursor is null ? string.Empty : $"&antesDe={cursor}")}";
            var pagina = await Actividades.LeerAsync(await e.Ana.GetAsync(e.RutaCampana(resto)));
            var ids = Elementos(pagina).Select(elemento => elemento.Id).ToList();
            Assert.Equal(14, pagina.GetProperty("sinLeer").GetInt32());
            Assert.True(ids.Count <= 4);
            vistos.AddRange(ids);
            cursor = pagina.GetProperty("hayMas").GetBoolean() ? ids[^1] : null;
            paginas++;
        }
        while (cursor is not null);

        var enOrden = esperados.Order(Comparer<Guid>.Create(OrdenDeIdentificadores.CompararDescendente)).ToList();
        Assert.Equal(enOrden, vistos);
        Assert.Equal(4, paginas);
    }

    [Fact]
    public async Task Marcar_todos_marca_comunicados_y_avisos_sin_tocar_a_otra_familia()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        await AvisarAsync(e, "Para las dos", DateTimeOffset.UtcNow, e.AnaJugador, e.Club.Caro.Integrante);
        await ComunicarAsync(e, "Para las dos", e.AnaJugador, e.Club.Caro.Integrante);
        Assert.Equal(2, await e.SinLeerAsync(e.Ana));

        var contador = await Actividades.LeerAsync(await e.Ana.PostAsync(e.RutaCampana("/lectura-total")));
        var otraVez = await Actividades.LeerAsync(await e.Ana.PostAsync(e.RutaCampana("/lectura-total")));

        Assert.Equal(0, contador.GetProperty("sinLeer").GetInt32());
        Assert.Equal(0, otraVez.GetProperty("sinLeer").GetInt32());
        Assert.Equal(0, await e.SinLeerAsync(e.Ana));
        Assert.Equal(2, await e.SinLeerAsync(e.Caro));
    }

    [Fact]
    public async Task Una_sesion_limitada_al_documento_de_un_hermano_solo_ve_lo_de_ese_jugador()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            e.AnaJugador, EstadoIngreso.APROBADO, categoria: e.Club.C2015, nombres: "Hugo");
        var deAna = await AvisarAsync(e, "De Ana", DateTimeOffset.UtcNow, e.AnaJugador);
        var deHugo = await AvisarAsync(e, "De Hugo", DateTimeOffset.UtcNow.AddSeconds(1), hugo);
        var limitada = await _fabrica.CrearClienteDePrueba().ConSesionLimitadaAsync(e.Club.Ana.Usuario, e.AnaJugador.Id);

        var vistaLimitada = await Actividades.LeerAsync(await limitada.GetAsync(e.RutaCampana()));
        var vistaCompleta = await Actividades.LeerAsync(await e.Ana.GetAsync(e.RutaCampana()));

        Assert.Equal([deAna.Id], Elementos(vistaLimitada).Select(elemento => elemento.Id).ToList());
        Assert.Equal(1, await e.SinLeerAsync(limitada));
        Assert.Equal(
            new[] { deAna.Id, deHugo.Id }.Order().ToList(),
            Elementos(vistaCompleta).Select(elemento => elemento.Id).Order().ToList());
        await Actividades.ErrorAsync(
            await limitada.PutAsync(e.RutaCampana($"/avisos/{deHugo.Id}/lectura"), new { }),
            HttpStatusCode.NotFound,
            "no_encontrado");
    }

    [Fact]
    public async Task El_equipo_del_club_recibe_403_y_el_desarrollador_y_otro_club_404()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var otro = await EscenarioComunicados.CrearAsync(_fabrica);
        var aviso = await AvisarAsync(e, "Aviso", DateTimeOffset.UtcNow, e.AnaJugador);

        foreach (var cliente in new[] { e.P, e.D, e.E1 })
        {
            await Actividades.ErrorAsync(
                await cliente.GetAsync(e.RutaCampana()), HttpStatusCode.Forbidden, "rol_no_autorizado");
            await Actividades.ErrorAsync(
                await cliente.GetAsync(e.RutaCampana("/contador")), HttpStatusCode.Forbidden, "rol_no_autorizado");
            await Actividades.ErrorAsync(
                await cliente.PostAsync(e.RutaCampana("/lectura-total")), HttpStatusCode.Forbidden, "rol_no_autorizado");
            await Actividades.ErrorAsync(
                await cliente.PutAsync(e.RutaCampana($"/avisos/{aviso.Id}/lectura"), new { }),
                HttpStatusCode.Forbidden,
                "rol_no_autorizado");
        }

        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await Actividades.ErrorAsync(
            await desarrollador.GetAsync(e.RutaCampana()), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await otro.Ana.GetAsync(e.RutaCampana()), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await otro.Ana.PutAsync(e.RutaCampana($"/avisos/{aviso.Id}/lectura"), new { }),
            HttpStatusCode.NotFound,
            "no_encontrado");
    }
}
