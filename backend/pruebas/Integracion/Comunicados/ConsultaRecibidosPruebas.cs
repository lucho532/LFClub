using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using static LaPecosa.Pruebas.Integracion.Comunicados.EscenarioComunicados;

namespace LaPecosa.Pruebas.Integracion.Comunicados;

/// <summary>
/// La familia nunca ve a los demás: lo que recibe no lleva la audiencia, los destinos ni cuántas
/// familias lo recibieron, y se pagina por cursor (spec 011, RF-032; research §9).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConsultaRecibidosPruebas
{
    private readonly FabricaApi _fabrica;

    public ConsultaRecibidosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_texto_de_la_respuesta_no_trae_audiencia_destinos_ni_familias()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var publicado = await e.PublicarAsync(e.P, Cuerpo("EQUIPOS", equipos: [e.Club.EquipoA]));
        var id = publicado.GetProperty("id").GetGuid();

        var lista = await e.Ana.GetAsync(e.RutaCampana());
        var detalle = await e.Ana.GetAsync(e.Ruta($"/recibidos/{id}"));

        foreach (var texto in new[] { await lista.Content.ReadAsStringAsync(), await detalle.Content.ReadAsStringAsync() })
        {
            Assert.Contains("Cambio de horario", texto);
            Assert.DoesNotContain("alcance", texto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("destinos", texto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("familias", texto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EQUIPOS", texto);
            Assert.DoesNotContain("puedeRetirar", texto);
            Assert.DoesNotContain("retirado", texto, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task El_detalle_no_marca_como_leido()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var publicado = await e.PublicarAsync(e.P, Cuerpo("CLUB"));
        var id = publicado.GetProperty("id").GetGuid();

        var detalle = await Actividades.LeerAsync(await e.Ana.GetAsync(e.Ruta($"/recibidos/{id}")));

        Assert.False(detalle.GetProperty("leido").GetBoolean());
        Assert.Equal(1, await e.SinLeerAsync(e.Ana));
    }

    [Fact]
    public async Task El_cursor_de_recibidos_no_repite_ni_salta_y_el_contador_acompana_cada_pagina()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var creados = new List<Guid>();
        for (var i = 0; i < 35; i++)
        {
            var comunicado = await _fabrica.Comunicados.PublicarAsync(
                e.Club.Club, e.Club.Presidente.Integrante, $"Aviso {i}", "Texto", AlcanceComunicado.CLUB,
                destinatarios: [e.AnaJugador]);
            creados.Add(comunicado.Id);
        }

        var vistos = new List<Guid>();
        Guid? cursor = null;
        do
        {
            var ruta = e.RutaCampana(cursor is null ? string.Empty : $"?antesDe={cursor}");
            var pagina = await Actividades.LeerAsync(await e.Ana.GetAsync(ruta));
            Assert.Equal(35, pagina.GetProperty("sinLeer").GetInt32());
            var ids = EscenarioComunicados.ComunicadosDe(pagina);
            vistos.AddRange(ids);
            cursor = pagina.GetProperty("hayMas").GetBoolean() ? ids[^1] : null;
        }
        while (cursor is not null);

        Assert.Equal(creados.OrderByDescending(id => id), vistos);
    }
}
