using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>El último pago registrado y el próximo cobro estimado que ve el PRESIDENTE (spec 017, historia 1).</summary>
[Collection(ColeccionApi.Nombre)]
public class ProximoPagoPresidentePruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public ProximoPagoPresidentePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public void Dispose()
    {
        _fabrica.Reloj.UsarElReal();
        _fabrica.CobrosPlataforma.QuitarTarifasAsync().GetAwaiter().GetResult();
    }

    private async Task<(Club Club, ClienteDePrueba Presidente)> ClubAsync(DateOnly? inicio, int jugadores, EstadoClub estado = EstadoClub.ACTIVO)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: estado);
        await _fabrica.CobrosPlataforma.FijarInicioAsync(club, inicio);
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        for (var i = 0; i < jugadores; i++)
        {
            await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        }

        // La sesión se abre con el reloj real: las sesiones se emiten con el reloj de la fábrica.
        _fabrica.Reloj.UsarElReal();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 3), new TimeOnly(10, 0));
        return (club, cliente);
    }

    private static async Task<JsonElement> ResumenAsync(Club club, ClienteDePrueba cliente) =>
        await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync($"/api/clubes/{club.Id}/pago-plataforma"));

    [Fact]
    public async Task El_resumen_trae_el_estado_de_pago_calculado_por_el_servidor()
    {
        var (alDia, p1) = await ClubAsync(new DateOnly(2026, 11, 1), 0);
        var (enPlazo, p2) = await ClubAsync(new DateOnly(2026, 11, 1), 0);
        await _fabrica.CobrosPlataforma.CrearCobroAsync(enPlazo, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var (vencido, p3) = await ClubAsync(new DateOnly(2026, 10, 1), 0);
        await _fabrica.CobrosPlataforma.CrearCobroAsync(vencido, new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 2));
        var (sinCobro, p4) = await ClubAsync(null, 0);

        Assert.Equal("AL_DIA", (await ResumenAsync(alDia, p1)).GetProperty("estadoDePago").GetString());
        Assert.Equal("PENDIENTE", (await ResumenAsync(enPlazo, p2)).GetProperty("estadoDePago").GetString());
        Assert.Equal("VENCIDO", (await ResumenAsync(vencido, p3)).GetProperty("estadoDePago").GetString());
        Assert.Equal("SIN_COBRO", (await ResumenAsync(sinCobro, p4)).GetProperty("estadoDePago").GetString());
    }

    [Fact]
    public async Task El_resumen_explica_la_tarifa_vigente_con_el_rango_del_club_y_los_dias_de_gracia()
    {
        await _fabrica.CobrosPlataforma.FijarRangosAsync([new(1, 2, 10000), new(3, null, 30000)], dias: 7);
        var (club, presidente) = await ClubAsync(null, jugadores: 3);

        var tarifa = (await ResumenAsync(club, presidente)).GetProperty("tarifa");

        Assert.Equal(7, tarifa.GetProperty("diasDeGracia").GetInt32());
        Assert.Equal(3, tarifa.GetProperty("jugadores").GetInt32());
        var rangos = tarifa.GetProperty("rangos").EnumerateArray().ToList();
        Assert.Equal(2, rangos.Count);
        Assert.Equal(10000, rangos[0].GetProperty("precio").GetDecimal());
        Assert.Equal(3, tarifa.GetProperty("rangoDelClub").GetProperty("desde").GetInt32());
        Assert.Equal(30000, tarifa.GetProperty("rangoDelClub").GetProperty("precio").GetDecimal());
    }

    [Fact]
    public async Task Sin_rangos_la_tarifa_viene_vacia_y_sin_rango_del_club()
    {
        var (club, presidente) = await ClubAsync(null, jugadores: 2);

        var tarifa = (await ResumenAsync(club, presidente)).GetProperty("tarifa");

        Assert.Empty(tarifa.GetProperty("rangos").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, tarifa.GetProperty("rangoDelClub").ValueKind);
    }

    [Fact]
    public async Task Sin_pagos_el_ultimo_pago_es_nulo()
    {
        var (club, presidente) = await ClubAsync(null, 0);
        await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));

        var resumen = await ResumenAsync(club, presidente);

        Assert.Equal(JsonValueKind.Null, resumen.GetProperty("ultimoPago").ValueKind);
    }

    [Fact]
    public async Task El_ultimo_pago_es_el_cobro_pagado_mas_recientemente_con_su_comprobante()
    {
        var (club, presidente) = await ClubAsync(null, 0);
        var octubre = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8), 40000, EstadoCobroPlataforma.PAGADO);
        var septiembre = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 8), 30000, EstadoCobroPlataforma.PAGADO);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.PagosPlataforma.Add(new PagoPlataforma
            {
                CobroId = octubre.Id, Valor = 40000, PagadoEn = new DateTime(2026, 10, 6, 15, 0, 0, DateTimeKind.Utc),
                Origen = OrigenPagoPlataforma.PASARELA, Referencia = "LFC-oct", TransaccionId = "tx-oct", MedioDePago = "PSE",
            });
            contexto.PagosPlataforma.Add(new PagoPlataforma
            {
                CobroId = septiembre.Id, Valor = 30000, PagadoEn = new DateTime(2026, 9, 5, 15, 0, 0, DateTimeKind.Utc),
                Origen = OrigenPagoPlataforma.MANUAL,
            });
            await contexto.SaveChangesAsync();
            return true;
        });

        var ultimo = (await ResumenAsync(club, presidente)).GetProperty("ultimoPago");

        Assert.Equal("2026-10", ultimo.GetProperty("periodo").GetString());
        Assert.Equal("LFC-oct", ultimo.GetProperty("referencia").GetString());
        Assert.Equal(40000, ultimo.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task El_proximo_cobro_estima_el_mes_siguiente_con_los_jugadores_de_hoy_y_la_tarifa_vigente()
    {
        await _fabrica.CobrosPlataforma.FijarRangosAsync([new(1, 2, 10000), new(3, null, 30000)]);
        var (club, presidente) = await ClubAsync(new DateOnly(2026, 11, 1), jugadores: 3);

        var proximo = (await ResumenAsync(club, presidente)).GetProperty("proximoCobro");

        Assert.Equal("2026-12", proximo.GetProperty("periodo").GetString());
        Assert.Equal(3, proximo.GetProperty("jugadores").GetInt32());
        Assert.Equal(30000, proximo.GetProperty("valorEstimado").GetDecimal());
        Assert.Equal(3, proximo.GetProperty("rango").GetProperty("desde").GetInt32());
        Assert.Equal(JsonValueKind.Null, proximo.GetProperty("rango").GetProperty("hasta").ValueKind);
    }

    [Fact]
    public async Task Si_el_cobro_empieza_mas_adelante_el_proximo_cobro_es_de_ese_mes()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(precio: 20000);
        var (club, presidente) = await ClubAsync(new DateOnly(2027, 3, 1), jugadores: 2);

        var proximo = (await ResumenAsync(club, presidente)).GetProperty("proximoCobro");

        Assert.Equal("2027-03", proximo.GetProperty("periodo").GetString());
    }

    [Fact]
    public async Task Sin_mes_de_inicio_sin_rangos_o_dado_de_baja_no_hay_proximo_cobro()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(precio: 20000);
        var (sinInicio, p1) = await ClubAsync(null, 2);
        var (dadoDeBaja, p2) = await ClubAsync(new DateOnly(2026, 11, 1), 2, EstadoClub.DADO_DE_BAJA);
        var (sinRangos, p3) = await ClubAsync(new DateOnly(2026, 11, 1), 2);
        await _fabrica.CobrosPlataforma.QuitarTarifasAsync();

        Assert.Equal(JsonValueKind.Null, (await ResumenAsync(sinInicio, p1)).GetProperty("proximoCobro").ValueKind);
        Assert.Equal(JsonValueKind.Null, (await ResumenAsync(sinRangos, p3)).GetProperty("proximoCobro").ValueKind);
        _ = dadoDeBaja;
        _ = p2;
    }
}
