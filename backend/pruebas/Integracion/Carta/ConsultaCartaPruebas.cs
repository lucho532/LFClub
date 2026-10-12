using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Carta;

/// <summary>
/// La carta la ven exactamente quienes ven la ficha, con los permisos de research §8; lo que no hay
/// se omite (spec 010, escenarios 3.1, 3.4, 3.6, 3.7 y 3.9; RF-019, RF-020, RF-028).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConsultaCartaPruebas
{
    private readonly FabricaApi _fabrica;

    public ConsultaCartaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public static string RutaCarta(EscenarioActividades e, Persona jugador) => e.Ruta($"/jugadores/{jugador.Id}/carta");

    private static (bool, bool, bool) Permisos(JsonElement carta)
    {
        var permisos = carta.GetProperty("permisos");
        return (
            permisos.GetProperty("cambiaEstadisticas").GetBoolean(),
            permisos.GetProperty("cambiaDorsalYPosicion").GetBoolean(),
            permisos.GetProperty("exporta").GetBoolean());
    }

    [Fact]
    public async Task La_ven_quienes_ven_la_ficha_con_sus_permisos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        var esperados = new (Persona Quien, (bool, bool, bool) Permisos)[]
        {
            (j3, (false, true, true)),
            (e.P, (true, true, true)),
            (e.E1, (true, true, false)),
            (e.E2, (true, true, false)),
            (e.D1, (true, true, false)),
            (e.D2, (false, false, false)),
        };

        foreach (var (quien, permisos) in esperados)
        {
            var respuesta = await quien.Cliente.GetAsync(RutaCarta(e, j3));
            Assert.True(respuesta.Headers.CacheControl is { Private: true, NoStore: true });
            var carta = await Actividades.LeerAsync(respuesta);
            Assert.Equal(j3.Id, carta.GetProperty("usuarioRolId").GetGuid());
            Assert.Equal("BRONCE", carta.GetProperty("rareza").GetString());
            Assert.Equal(permisos, Permisos(carta));
        }
    }

    [Fact]
    public async Task Quien_no_ve_la_ficha_recibe_lo_mismo_que_con_un_identificador_inventado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var inventado = await (await e.Jn(4).Cliente.GetAsync(e.Ruta($"/jugadores/{Guid.NewGuid()}/carta"))).Content.ReadAsStringAsync();

        var respuestas = new[]
        {
            await e.Jn(4).Cliente.GetAsync(RutaCarta(e, e.Jn(3))),
            await e.E1.Cliente.GetAsync(RutaCarta(e, e.K1)),
            await e.PB.Cliente.GetAsync(RutaCarta(e, e.Jn(3))),
            await desarrollador.GetAsync(RutaCarta(e, e.Jn(3))),
        };

        foreach (var respuesta in respuestas)
        {
            await Actividades.ErrorAsync(respuesta, HttpStatusCode.NotFound, "no_encontrado");
        }

        Assert.Equal(inventado, await respuestas[0].Content.ReadAsStringAsync());
    }

    // Como el resto de endpoints del club: en un club suspendido, solo el PRESIDENTE sigue entrando.
    [Fact]
    public async Task En_un_club_suspendido_los_cinco_endpoints_avisan_a_los_demas()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.ConContextoAsync(contexto => contexto.Clubes.Where(club => club.Id == e.Club.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(club => club.Estado, EstadoClub.SUSPENDIDO)));
        var carta = RutaCarta(e, e.Jn(3));
        var x = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], _fabrica.Reloj.Hoy.AddDays(-1), convocados: [e.Jn(3).Integrante]);

        foreach (var persona in new[] { e.Jn(3), e.E1, e.D1 })
        {
            foreach (var respuesta in new[]
            {
                await persona.Cliente.GetAsync(carta),
                await persona.Cliente.GetAsync($"{carta}/imagen"),
                await persona.Cliente.PutAsync($"{carta}/estadisticas", new { ritmo = (int?)null }),
                await persona.Cliente.PutAsync($"{carta}/dorsal-y-posicion", new { dorsal = 10 }),
                await persona.Cliente.PutAsync(e.Ruta($"/partidos/{x.Id}/resultado/mi-jugador"), new { jugo = false, goles = 0 }),
            })
            {
                await Actividades.ErrorAsync(respuesta, HttpStatusCode.Forbidden, "club_suspendido");
            }
        }

        await Actividades.LeerAsync(await e.P.Cliente.GetAsync(carta));
    }

    [Fact]
    public async Task Sin_carta_guardada_ni_foto_no_lleva_nada_de_eso()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Cartas.PonerColoresYEscudoAsync(e.Club, "#1B3A6B", "#F2C200");

        var texto = await (await e.P.Cliente.GetAsync(RutaCarta(e, e.Jn(4)))).Content.ReadAsStringAsync();

        foreach (var ausente in new[] { "dorsal", "posicion", "estadisticas", "valoracion", "fotoActualizadaEn", "ultimoCambioEstadisticas" })
        {
            Assert.DoesNotContain($"\"{ausente}\"", texto);
        }

        // Escudo y colores son los de la identidad del club, no de la carta (escenario 3.9).
        Assert.DoesNotContain("color", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("escudo", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("porcentaje", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Con_carta_y_foto_las_muestra_y_el_ultimo_cambio_solo_a_quien_puede_cambiarlas()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        await _fabrica.Cartas.PonerFotoAsync(j3.Integrante);
        await _fabrica.Cartas.PonerCartaAsync(j3.Integrante, 10, PosicionJugador.DELANTERO, [70, 65, 80, 72, 60, 88]);

        var deP = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(RutaCarta(e, j3)));

        Assert.Equal(10, deP.GetProperty("dorsal").GetInt32());
        Assert.Equal("DELANTERO", deP.GetProperty("posicion").GetString());
        Assert.Equal(73, deP.GetProperty("valoracion").GetInt32());
        Assert.Equal(88, deP.GetProperty("estadisticas").GetProperty("fisico").GetInt32());
        Assert.True(deP.TryGetProperty("fotoActualizadaEn", out _));
        Assert.Equal("Sembrador", deP.GetProperty("ultimoCambioEstadisticas").GetProperty("autor").GetString());
        foreach (var quien in new[] { j3, e.D2 })
        {
            var carta = await Actividades.LeerAsync(await quien.Cliente.GetAsync(RutaCarta(e, j3)));
            Assert.Equal(73, carta.GetProperty("valoracion").GetInt32());
            Assert.False(carta.TryGetProperty("ultimoCambioEstadisticas", out _));
        }
    }
}
