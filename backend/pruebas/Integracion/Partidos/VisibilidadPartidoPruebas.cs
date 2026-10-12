using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Partidos;

/// <summary>
/// Un partido próximo solo existe para sus convocados, quienes entrenan alguna de sus categorías y
/// el PRESIDENTE: para cualquier otra cuenta no aparece en ninguna lista, en ningún filtro ni en el
/// calendario, y pedirlo responde exactamente lo mismo que un identificador inventado (constitución
/// §17.2; historia 2, escenarios 2.2 a 2.5; RF-010 a RF-012; CE-001 y CE-002).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class VisibilidadPartidoPruebas
{
    private static readonly int[] Convocados = [1, 3, 4, 5, 6, 7, 8];

    private readonly FabricaApi _fabrica;

    public VisibilidadPartidoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    [Fact]
    public async Task Las_diez_cuentas_de_la_2014_solo_lo_encuentran_si_estan_convocadas()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await PartidoConvocadoAsync(e);

        for (var numero = 1; numero <= 10; numero++)
        {
            await ComprobarAsync(e, e.Jn(numero), partido, Convocados.Contains(numero), $"J{numero}");
        }
    }

    [Fact]
    public async Task Quien_entrena_y_el_presidente_lo_ven_y_el_directivo_sin_asignar_no()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await PartidoConvocadoAsync(e);

        foreach (var (persona, nombre) in new[] { (e.D1, "D1"), (e.E1, "E1"), (e.E2, "E2"), (e.P, "P") })
        {
            await ComprobarAsync(e, persona, partido, true, nombre);
        }

        await ComprobarAsync(e, e.D2, partido, false, "D2");
        await ComprobarAsync(e, e.M1, partido, false, "M1");
        await Actividades.ErrorAsync(
            await e.PB.Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}")), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Un_convocado_no_recibe_convocados_ni_jugadores_en_el_texto()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await PartidoConvocadoAsync(e);

        var texto = await (await e.Jn(3).Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}"))).Content.ReadAsStringAsync();
        var lista = await (await e.Jn(3).Cliente.GetAsync(e.Ruta("/partidos"))).Content.ReadAsStringAsync();

        foreach (var cuerpo in new[] { texto, lista })
        {
            Assert.DoesNotContain("convocados", cuerpo);
            Assert.DoesNotContain("convocables", cuerpo);
            Assert.DoesNotContain("jugadores", cuerpo);
            Assert.DoesNotContain(e.Jn(4).Integrante.Apellidos, cuerpo);
        }
    }

    private Task<Partido> PartidoConvocadoAsync(EscenarioActividades e) =>
        _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], Hoy.AddDays(2), convocados: Convocados.Select(numero => e.Jn(numero).Integrante));

    /// <summary>
    /// El calendario, la lista (sin filtro, por estado y por categoría) y la consulta por
    /// identificador dicen lo mismo; quien no lo ve recibe lo mismo que por un identificador inventado.
    /// </summary>
    private async Task ComprobarAsync(EscenarioActividades e, Persona persona, Partido partido, bool loVe, string quien)
    {
        var cliente = persona.Cliente;
        var calendario = await Actividades.LeerAsync(await cliente.GetAsync(
            e.Ruta($"/calendario?desde={Actividades.Texto(Hoy)}&hasta={Actividades.Texto(Hoy.AddDays(6))}")));
        var listas = new[]
        {
            calendario.GetProperty("partidos"),
            await Actividades.LeerAsync(await cliente.GetAsync(e.Ruta("/partidos"))),
            await Actividades.LeerAsync(await cliente.GetAsync(e.Ruta("/partidos?estado=PROXIMO"))),
            await Actividades.LeerAsync(await cliente.GetAsync(e.Ruta($"/partidos?categoriaId={e.C2014.Id}"))),
        };

        foreach (var lista in listas)
        {
            Assert.True(loVe == Actividades.Ids(lista, "partidoId").Contains(partido.Id), $"{quien}: lista");
            Assert.Equal(loVe ? 1 : 0, lista.GetArrayLength());
        }

        var detalle = await cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}"));
        if (loVe)
        {
            Assert.True(detalle.StatusCode == HttpStatusCode.OK, $"{quien}: detalle {(int)detalle.StatusCode}");
            return;
        }

        var inventado = await cliente.GetAsync(e.Ruta($"/partidos/{Guid.NewGuid()}"));
        Assert.True(detalle.StatusCode == HttpStatusCode.NotFound, $"{quien}: detalle {(int)detalle.StatusCode}");
        Assert.Equal(inventado.StatusCode, detalle.StatusCode);
        Assert.Equal(await inventado.Content.ReadAsStringAsync(), await detalle.Content.ReadAsStringAsync());
    }
}
