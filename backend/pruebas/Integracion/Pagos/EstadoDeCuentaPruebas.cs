using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// El estado de cuenta de un jugador: cargos, pagos, saldo y estado; quién lo ve y quién no; y que deber
/// no restringe nada (spec 015, historia 3; RF-001 a RF-004, RF-015, RF-025, RF-035; CE-002, CE-005).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EstadoDeCuentaPruebas
{
    private readonly FabricaApi _fabrica;

    public EstadoDeCuentaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private async Task<JsonElement> CuentaAsync(Usuario quien, ClubDePagos c, UsuarioRol jugador)
    {
        var respuesta = await (await ComoAsync(quien)).GetAsync(c.Cuenta(jugador));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
    }

    /// <summary>Ana debe dos mensualidades de 65.000 y ha abonado 90.000; Beto no ha pagado nada.</summary>
    private async Task<(ClubDePagos Club, Cargo Septiembre, Cargo Octubre)> ClubConDeudasAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var septiembre = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 9, 1));
        var octubre = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        await _fabrica.Pagos.CrearCargoAsync(c.Beto.Integrante, new DateOnly(2026, 10, 1));
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(
            c.Ana.Integrante, c.Presidente.Integrante, 90000m, septiembre, octubre);
        return (c, septiembre, octubre);
    }

    [Fact]
    public async Task Dos_mensualidades_y_un_abono_de_90000_dejan_40000_y_pago_parcial()
    {
        var (c, _, _) = await ClubConDeudasAsync();

        var cuenta = await CuentaAsync(c.Presidente.Usuario, c, c.Ana.Integrante);

        Assert.Equal(40000m, cuenta.GetProperty("saldo").GetDecimal());
        Assert.Equal("PAGO_PARCIAL", cuenta.GetProperty("estado").GetString());
        var cargos = cuenta.GetProperty("cargos").EnumerateArray().ToList();
        Assert.Equal(2, cargos.Count);
        Assert.Equal(new[] { 0m, 40000m }.Order(), cargos.Select(cargo => cargo.GetProperty("pendiente").GetDecimal()).Order());
        Assert.Equal(new[] { 65000m, 25000m }.Order(), cargos.Select(cargo => cargo.GetProperty("pagado").GetDecimal()).Order());
        var pago = Assert.Single(cuenta.GetProperty("pagos").EnumerateArray());
        Assert.Equal("CONFIRMADO", pago.GetProperty("estado").GetString());
        Assert.Equal(90000m, pago.GetProperty("valor").GetDecimal());
    }

    [Fact]
    public async Task Los_tres_estados_se_calculan_de_los_cargos_y_los_pagos_confirmados()
    {
        var (c, _, octubre) = await ClubConDeudasAsync();
        var sinCargos = await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2014, c.Categoria, "Caro");
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 40000m, octubre);

        Assert.Equal("DEBE", (await CuentaAsync(c.Presidente.Usuario, c, c.Beto.Integrante)).GetProperty("estado").GetString());
        Assert.Equal("AL_DIA", (await CuentaAsync(c.Presidente.Usuario, c, c.Ana.Integrante)).GetProperty("estado").GetString());
        var vacia = await CuentaAsync(c.Presidente.Usuario, c, sinCargos.Integrante);
        Assert.Equal("AL_DIA", vacia.GetProperty("estado").GetString());
        Assert.Equal(0m, vacia.GetProperty("saldo").GetDecimal());
    }

    [Fact]
    public async Task Un_pago_anulado_o_en_revision_no_suma_pero_se_ve_en_el_historial()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var anulado = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        await _fabrica.Pagos.AnularAsync(anulado, c.Presidente.Integrante);
        await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);

        var cuenta = await CuentaAsync(c.Presidente.Usuario, c, c.Ana.Integrante);

        Assert.Equal(65000m, cuenta.GetProperty("saldo").GetDecimal());
        Assert.Equal("DEBE", cuenta.GetProperty("estado").GetString());
        var estados = cuenta.GetProperty("pagos").EnumerateArray().Select(p => p.GetProperty("estado").GetString()).Order();
        Assert.Equal(new[] { "ANULADO", "EN_REVISION" }, estados);
        var enRevision = cuenta.GetProperty("pagos").EnumerateArray().Single(p => p.GetProperty("estado").GetString() == "EN_REVISION");
        Assert.True(enRevision.GetProperty("tieneSoporte").GetBoolean());
        Assert.False(enRevision.GetProperty("tieneResguardo").GetBoolean());
    }

    [Fact]
    public async Task Una_mensualidad_sin_pagar_despues_del_dia_5_sigue_pendiente_sin_ninguna_consecuencia()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var ana = await ComoAsync(c.Ana.Usuario);
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 11, 1));
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 28), new TimeOnly(10, 0));
        try
        {
            var cuenta = await ClienteDePrueba.LeerAsync<JsonElement>(await ana.GetAsync(c.Cuenta(c.Ana.Integrante)));

            Assert.Equal("DEBE", cuenta.GetProperty("estado").GetString());
            Assert.Equal(65000m, cuenta.GetProperty("saldo").GetDecimal());
            // Deber no cambia lo que ve ni lo que hace: sigue entrando a lo demás del club (RF-015).
            Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync(c.Ruta("/mi-categoria"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync(c.Ruta("/calendario?desde=2026-11-23&hasta=2026-11-29"))).StatusCode);
        }
        finally
        {
            _fabrica.Reloj.UsarElReal();
        }
    }

    [Fact]
    public async Task El_presidente_y_el_directivo_ven_cualquier_jugador_tambien_retirado()
    {
        var (c, _, _) = await ClubConDeudasAsync();
        await _fabrica.Categorias.RetirarAsync(c.Beto.Integrante, c.Presidente.Integrante);

        foreach (var quien in new[] { c.Presidente.Usuario, c.Directivo.Usuario })
        {
            Assert.Equal("DEBE", (await CuentaAsync(quien, c, c.Beto.Integrante)).GetProperty("estado").GetString());
            Assert.Equal("PAGO_PARCIAL", (await CuentaAsync(quien, c, c.Ana.Integrante)).GetProperty("estado").GetString());
        }
    }

    [Fact]
    public async Task Las_banderas_dicen_quien_puede_registrar_y_quien_puede_informar()
    {
        var (c, _, _) = await ClubConDeudasAsync();

        var presidente = await CuentaAsync(c.Presidente.Usuario, c, c.Ana.Integrante);
        var directivo = await CuentaAsync(c.Directivo.Usuario, c, c.Ana.Integrante);
        var familia = await CuentaAsync(c.Ana.Usuario, c, c.Ana.Integrante);

        Assert.True(presidente.GetProperty("puedeRegistrar").GetBoolean());
        Assert.False(presidente.GetProperty("puedeInformar").GetBoolean());
        Assert.False(directivo.GetProperty("puedeRegistrar").GetBoolean());
        Assert.False(directivo.GetProperty("puedeInformar").GetBoolean());
        Assert.False(familia.GetProperty("puedeRegistrar").GetBoolean());
        Assert.True(familia.GetProperty("puedeInformar").GetBoolean());
    }

    [Fact]
    public async Task La_familia_solo_ve_la_cuenta_de_su_jugador_incluso_por_identificador()
    {
        var (c, _, _) = await ClubConDeudasAsync();
        var otroClub = await _fabrica.Pagos.CrearClubAsync();
        var ana = await ComoAsync(c.Ana.Usuario);

        var deBeto = await ana.GetAsync(c.Cuenta(c.Beto.Integrante));
        var deOtroClub = await ana.GetAsync(otroClub.Cuenta(otroClub.Ana.Integrante));
        var inexistente = await ana.GetAsync(c.Cuenta(new UsuarioRol { Id = Guid.NewGuid() }));

        foreach (var respuesta in new[] { deBeto, deOtroClub, inexistente })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task Un_jugador_retirado_ya_no_ve_su_cuenta()
    {
        var (c, _, _) = await ClubConDeudasAsync();
        var ana = await ComoAsync(c.Ana.Usuario);
        await _fabrica.Categorias.RetirarAsync(c.Ana.Integrante, c.Presidente.Integrante);

        var respuesta = await ana.GetAsync(c.Cuenta(c.Ana.Integrante));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_entrenador_recibe_403_tambien_para_los_jugadores_de_su_categoria()
    {
        var (c, _, _) = await ClubConDeudasAsync();
        var entrenador = await ComoAsync(c.Entrenador.Usuario);

        var respuesta = await entrenador.GetAsync(c.Cuenta(c.Ana.Integrante));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.DoesNotContain("saldo", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_desarrollador_no_tiene_ruta_de_club_y_recibe_404()
    {
        var (c, _, _) = await ClubConDeudasAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await desarrollador.GetAsync(c.Cuenta(c.Ana.Integrante));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Quien_no_es_jugador_no_tiene_cuenta()
    {
        var (c, _, _) = await ClubConDeudasAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.GetAsync(c.Cuenta(c.Directivo.Integrante));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
