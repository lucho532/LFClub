using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// El PRESIDENTE y el DIRECTIVO ven el recaudo del mes, la cartera, los totales por categoría y quiénes
/// deben; el ENTRENADOR no ve nada (spec 015, historia 9; RF-031 a RF-034 y RF-037; CE-007 y CE-009).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FinanzasPruebas
{
    private readonly FabricaApi _fabrica;

    public FinanzasPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private DateOnly MesActual => _fabrica.Pagos.PrimerDiaDelMes();

    private async Task<JsonElement> FinanzasAsync(ClubDePagos c, string consulta = "", Usuario? quien = null)
    {
        var respuesta = await (await ComoAsync(quien ?? c.Presidente.Usuario)).GetAsync(c.Ruta($"/finanzas{consulta}"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
    }

    /// <summary>
    /// Ana y Beto de la 2014 y Caro de la 2015, cada uno con la mensualidad del mes en curso (65.000): Ana
    /// la pagó entera, Beto nada y Caro 20.000.
    /// </summary>
    private async Task<(ClubDePagos Club, UsuarioRol Caro, Categoria C2015, Pago PagoAna)> ClubConTresJugadoresAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var c2015 = await _fabrica.Categorias.CrearCategoriaAsync(c.Club, 2015);
        var caro = (await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2015, c2015, "Caro")).Integrante;
        var cargoAna = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, MesActual);
        await _fabrica.Pagos.CrearCargoAsync(c.Beto.Integrante, MesActual);
        var cargoCaro = await _fabrica.Pagos.CrearCargoAsync(caro, MesActual);
        var pagoAna = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargoAna);
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(caro, c.Presidente.Integrante, 20000m, cargoCaro);
        return (c, caro, c2015, pagoAna);
    }

    [Fact]
    public async Task El_recaudo_la_cartera_los_totales_por_categoria_y_quien_debe_coinciden_con_los_cargos_y_los_pagos()
    {
        var (c, caro, _, _) = await ClubConTresJugadoresAsync();

        var finanzas = await FinanzasAsync(c);

        Assert.Equal(MesActual.ToString("yyyy-MM-dd"), finanzas.GetProperty("mes").GetString());
        Assert.Equal(85000m, finanzas.GetProperty("recaudoMes").GetDecimal());
        Assert.Equal(110000m, finanzas.GetProperty("carteraPendiente").GetDecimal());
        var categorias = finanzas.GetProperty("porCategoria").EnumerateArray().ToList();
        Assert.Equal(2, categorias.Count);
        Assert.Equal("Categoría 2014", categorias[0].GetProperty("nombre").GetString());
        Assert.Equal(65000m, categorias[0].GetProperty("recaudoMes").GetDecimal());
        Assert.Equal(65000m, categorias[0].GetProperty("cartera").GetDecimal());
        Assert.Equal("Categoría 2015", categorias[1].GetProperty("nombre").GetString());
        Assert.Equal(20000m, categorias[1].GetProperty("recaudoMes").GetDecimal());
        Assert.Equal(45000m, categorias[1].GetProperty("cartera").GetDecimal());
        var deben = finanzas.GetProperty("jugadoresQueDeben").EnumerateArray().ToList();
        Assert.Equal([c.Beto.Integrante.Id, caro.Id], deben.Select(fila => fila.GetProperty("usuarioRolId").GetGuid()));
        Assert.Equal("DEBE", deben[0].GetProperty("estado").GetString());
        Assert.Equal("PAGO_PARCIAL", deben[1].GetProperty("estado").GetString());
        Assert.Equal(45000m, deben[1].GetProperty("saldo").GetDecimal());
    }

    [Fact]
    public async Task El_recaudo_solo_cuenta_los_pagos_confirmados_del_mes_no_los_en_revision_ni_los_anulados()
    {
        var (c, caro, _, pagoAna) = await ClubConTresJugadoresAsync();
        await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Beto.Integrante, 65000m, conSoporte: true);

        var conRevision = await FinanzasAsync(c);
        await _fabrica.Pagos.AnularAsync(pagoAna, c.Presidente.Integrante);
        var anulado = await FinanzasAsync(c);

        Assert.Equal(85000m, conRevision.GetProperty("recaudoMes").GetDecimal());
        Assert.Equal(1, conRevision.GetProperty("pagosEnRevision").GetInt32());
        Assert.Equal(20000m, anulado.GetProperty("recaudoMes").GetDecimal());
        Assert.Equal(175000m, anulado.GetProperty("carteraPendiente").GetDecimal());
        Assert.Equal(3, anulado.GetProperty("jugadoresQueDeben").GetArrayLength());
        Assert.NotEqual(Guid.Empty, caro.Id);
    }

    [Fact]
    public async Task El_mes_se_mide_en_hora_de_Colombia_y_se_puede_pedir_otro_mes()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 9, 1));
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        // 1 de noviembre 02:00 UTC es el 31 de octubre a las 21:00 en Bogotá: cuenta en octubre.
        await _fabrica.Pagos.FijarConfirmadoEnAsync(pago, new DateTime(2026, 11, 1, 2, 0, 0, DateTimeKind.Utc));

        var octubre = await FinanzasAsync(c, "?mes=2026-10-01");
        var noviembre = await FinanzasAsync(c, "?mes=2026-11-01");

        Assert.Equal(65000m, octubre.GetProperty("recaudoMes").GetDecimal());
        Assert.Equal(0m, noviembre.GetProperty("recaudoMes").GetDecimal());
        Assert.Equal("2026-10-01", octubre.GetProperty("mes").GetString());
    }

    [Theory]
    [InlineData("?mes=2026-10-15")]
    [InlineData("?mes=octubre")]
    public async Task Un_mes_que_no_es_el_primer_dia_del_mes_responde_400(string consulta)
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var respuesta = await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta($"/finanzas{consulta}"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task La_cartera_incluye_a_los_retirados_que_deben_y_no_cuenta_cuentas_eliminadas()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, MesActual);
        await _fabrica.Pagos.CrearCargoAsync(c.Beto.Integrante, MesActual);
        await _fabrica.Categorias.RetirarAsync(c.Beto.Integrante, c.Presidente.Integrante);
        await (await ComoAsync(c.Ana.Usuario)).PostAsync("/api/cuenta/eliminacion", new { contrasena = Sembrador.Contrasena });

        var finanzas = await FinanzasAsync(c);

        Assert.Equal(65000m, finanzas.GetProperty("carteraPendiente").GetDecimal());
        var deben = Assert.Single(finanzas.GetProperty("jugadoresQueDeben").EnumerateArray());
        Assert.Equal(c.Beto.Integrante.Id, deben.GetProperty("usuarioRolId").GetGuid());
        Assert.True(deben.GetProperty("retirado").GetBoolean());
    }

    [Fact]
    public async Task Un_cambio_de_categoria_no_reordena_hacia_atras_los_totales()
    {
        var (c, _, c2015, _) = await ClubConTresJugadoresAsync();

        await _fabrica.Pagos.CambiarCategoriaAsync(c.Beto.Integrante, c2015.Id);
        var finanzas = await FinanzasAsync(c);

        var c2014 = finanzas.GetProperty("porCategoria").EnumerateArray().Single(f => f.GetProperty("nombre").GetString() == "Categoría 2014");
        Assert.Equal(65000m, c2014.GetProperty("cartera").GetDecimal());
    }

    [Fact]
    public async Task Los_cargos_sin_categoria_van_a_sin_categoria_al_final()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CambiarCategoriaAsync(c.Beto.Integrante, null);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.Cargos.Add(new Cargo
            {
                ClubId = c.Club.Id, UsuarioRolId = c.Beto.Integrante.Id, Tipo = LFClub.Dominio.Enumeraciones.TipoCargo.MENSUALIDAD,
                Mes = MesActual, Valor = 65000m, CategoriaId = null, CreadoEn = DateTime.UtcNow,
            });
            return await contexto.SaveChangesAsync();
        });
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, MesActual);

        var categorias = (await FinanzasAsync(c)).GetProperty("porCategoria").EnumerateArray().ToList();

        Assert.Equal(["Categoría 2014", "Sin categoría"], categorias.Select(f => f.GetProperty("nombre").GetString()));
        Assert.Equal(JsonValueKind.Null, categorias[1].GetProperty("categoriaId").ValueKind);
    }

    [Fact]
    public async Task El_directivo_consulta_y_ve_lo_mismo_que_el_presidente()
    {
        var (c, _, _, _) = await ClubConTresJugadoresAsync();

        var deDirectivo = await FinanzasAsync(c, quien: c.Directivo.Usuario);
        var dePresidente = await FinanzasAsync(c);

        Assert.Equal(dePresidente.GetRawText(), deDirectivo.GetRawText());
    }

    [Fact]
    public async Task El_entrenador_y_la_familia_reciben_403_y_el_desarrollador_404()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        foreach (var quien in new[] { c.Entrenador.Usuario, c.Ana.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).GetAsync(c.Ruta("/finanzas"));

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.DoesNotContain("recaudo", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await desarrollador.GetAsync(c.Ruta("/finanzas"))).StatusCode);
    }

    [Fact]
    public async Task Ningun_dato_de_otro_club_aparece()
    {
        var (c, _, _, _) = await ClubConTresJugadoresAsync();
        var otro = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(otro.Ana.Integrante, MesActual);
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(otro.Ana.Integrante, otro.Presidente.Integrante, 40000m, cargo);

        var finanzas = await FinanzasAsync(c);
        var delOtro = await FinanzasAsync(otro);

        Assert.Equal(85000m, finanzas.GetProperty("recaudoMes").GetDecimal());
        Assert.Equal(40000m, delOtro.GetProperty("recaudoMes").GetDecimal());
        Assert.DoesNotContain(
            otro.Ana.Integrante.Id,
            finanzas.GetProperty("jugadoresQueDeben").EnumerateArray().Select(f => f.GetProperty("usuarioRolId").GetGuid()));
        Assert.Equal(HttpStatusCode.NotFound, (await (await ComoAsync(otro.Presidente.Usuario)).GetAsync(c.Ruta("/finanzas"))).StatusCode);
    }

    [Fact]
    public async Task Un_club_sin_movimientos_responde_todo_en_cero()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var finanzas = await FinanzasAsync(c);

        Assert.Equal(0m, finanzas.GetProperty("recaudoMes").GetDecimal());
        Assert.Equal(0m, finanzas.GetProperty("carteraPendiente").GetDecimal());
        Assert.Equal(0, finanzas.GetProperty("porCategoria").GetArrayLength());
        Assert.Equal(0, finanzas.GetProperty("jugadoresQueDeben").GetArrayLength());
    }
}
