using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// El PRESIDENTE revisa las transferencias informadas: las lista del más antiguo al más reciente, las
/// confirma (con el valor y los cargos que decida) o las rechaza con un motivo que la familia ve (spec
/// 015, historia 6; RF-022, RF-023; clarificaciones 3 y 4).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RevisionDePagosPruebas
{
    private readonly FabricaApi _fabrica;

    public RevisionDePagosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private async Task<(ClubDePagos Club, Cargo Septiembre, Cargo Octubre)> ClubConDosCargosAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var septiembre = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 9, 1));
        var octubre = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        return (c, septiembre, octubre);
    }

    private async Task<JsonElement> EstadoDeCuentaAsync(ClubDePagos c, UsuarioRol jugador) =>
        await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Cuenta(jugador)));

    [Fact]
    public async Task La_lista_va_del_mas_antiguo_al_mas_reciente_con_el_total_y_los_cargos_pendientes()
    {
        var (c, _, _) = await ClubConDosCargosAsync();
        var primero = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 30000m, conSoporte: true);
        await Task.Delay(20);
        var segundo = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);

        foreach (var quien in new[] { c.Presidente.Usuario, c.Directivo.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).GetAsync(c.Ruta("/pagos/en-revision"));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var lista = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
            Assert.Equal(2, lista.GetProperty("total").GetInt32());
            var pagos = lista.GetProperty("pagos").EnumerateArray().ToList();
            Assert.Equal(
                new[] { primero.Id, segundo.Id },
                pagos.Select(fila => fila.GetProperty("pago").GetProperty("id").GetGuid()));
            Assert.Equal(2, pagos[0].GetProperty("cargosPendientes").GetArrayLength());
            Assert.Equal($"{c.Ana.Integrante.Nombres} {c.Ana.Integrante.Apellidos}", pagos[0].GetProperty("jugadorNombre").GetString());
        }
    }

    [Fact]
    public async Task La_lista_solo_trae_los_pagos_en_revision_del_club()
    {
        var (c, _, _) = await ClubConDosCargosAsync();
        var otro = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearPagoEnRevisionAsync(otro.Club, otro.Ana.Integrante, 1000m);
        await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 1000m);
        var cargo = (await _fabrica.Pagos.CargosDelClubAsync(c.Club)).First();
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 1000m, cargo);

        var lista = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta("/pagos/en-revision")));

        Assert.Equal(1, lista.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Confirmar_aplica_solo_lo_pendiente_da_recibo_y_deja_al_jugador_al_dia()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var pago = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("CONFIRMADO", pago.GetProperty("estado").GetString());
        Assert.Equal(1, pago.GetProperty("numeroRecibo").GetInt32());
        Assert.Equal(0m, pago.GetProperty("saldoTrasPago").GetDecimal());
        Assert.Equal(JsonValueKind.Null, pago.GetProperty("valorInformado").ValueKind);
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
        Assert.Equal("AL_DIA", (await EstadoDeCuentaAsync(c, c.Ana.Integrante)).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Confirmar_con_otro_valor_lo_corrige_y_guarda_lo_que_informo_la_familia()
    {
        var (c, septiembre, octubre) = await ClubConDosCargosAsync();
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(
            c.Ruta($"/pagos/{informado.Id}/confirmacion"), new { valor = 40000, cargoIds = new[] { octubre.Id } });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var pago = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(40000m, pago.GetProperty("valor").GetDecimal());
        Assert.Equal(65000m, pago.GetProperty("valorInformado").GetDecimal());
        Assert.Equal(40000m, await _fabrica.Pagos.PagadoAsync(octubre));
        Assert.Equal(0m, await _fabrica.Pagos.PagadoAsync(septiembre));
        Assert.Equal(90000m, pago.GetProperty("saldoTrasPago").GetDecimal());
    }

    [Fact]
    public async Task Confirmar_algo_ya_pagado_por_otra_via_aplica_solo_lo_pendiente_o_responde_409()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 25000m, cargo);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var excede = await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion"));
        var corregido = await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion"), new { valor = 40000 });

        Assert.Equal(HttpStatusCode.Conflict, excede.StatusCode);
        Assert.Equal("pago_excede_deuda", await ClienteDePrueba.CodigoAsync(excede));
        Assert.Equal(HttpStatusCode.OK, corregido.StatusCode);
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task Rechazar_exige_un_motivo_lo_guarda_y_no_cambia_ningun_saldo()
    {
        var (c, _, _) = await ClubConDosCargosAsync();
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var sinMotivo = await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/rechazo"), new { motivo = "  " });
        var rechazo = await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/rechazo"), new { motivo = "La imagen no se ve" });

        Assert.Equal(HttpStatusCode.BadRequest, sinMotivo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rechazo.StatusCode);
        var pago = await ClienteDePrueba.LeerAsync<JsonElement>(rechazo);
        Assert.Equal("RECHAZADO", pago.GetProperty("estado").GetString());
        Assert.Equal("La imagen no se ve", pago.GetProperty("motivoRechazo").GetString());
        Assert.Equal($"{c.Presidente.Integrante.Nombres} {c.Presidente.Integrante.Apellidos}", pago.GetProperty("rechazadoPorNombre").GetString());
        var cuenta = await EstadoDeCuentaAsync(c, c.Ana.Integrante);
        Assert.Equal(130000m, cuenta.GetProperty("saldo").GetDecimal());

        // La familia ve el rechazo con su motivo.
        var familia = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(c.Ana.Usuario)).GetAsync(c.Cuenta(c.Ana.Integrante)));
        var enLaCuenta = Assert.Single(familia.GetProperty("pagos").EnumerateArray());
        Assert.Equal("La imagen no se ve", enLaCuenta.GetProperty("motivoRechazo").GetString());
    }

    [Fact]
    public async Task Un_pago_rechazado_no_se_reabre_y_la_familia_informa_uno_nuevo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/rechazo"), new { motivo = "No llegó" });

        var confirmar = await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion"));
        var rechazarOtraVez = await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/rechazo"), new { motivo = "Otra vez" });
        var nuevo = await (await ComoAsync(c.Ana.Usuario)).PostFormularioAsync(
            c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/pagos/informados"),
            new Dictionary<string, string>
            {
                ["valor"] = "65000", ["entidad"] = "Nequi", ["fecha"] = _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd"),
            },
            Imagenes.Png());

        Assert.Equal(HttpStatusCode.Conflict, confirmar.StatusCode);
        Assert.Equal("pago_no_esta_en_revision", await ClienteDePrueba.CodigoAsync(confirmar));
        Assert.Equal("pago_no_esta_en_revision", await ClienteDePrueba.CodigoAsync(rechazarOtraVez));
        Assert.Equal(HttpStatusCode.Created, nuevo.StatusCode);
    }

    [Fact]
    public async Task Dos_confirmaciones_a_la_vez_del_mismo_pago_dejan_una_sola_aplicacion()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        var uno = await ComoAsync(c.Presidente.Usuario);
        var otro = await ComoAsync(c.Presidente.Usuario);

        var respuestas = await Task.WhenAll(
            uno.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion")),
            otro.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion")));

        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task El_directivo_el_entrenador_y_la_familia_no_confirman_ni_rechazan()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);

        foreach (var quien in new[] { c.Directivo.Usuario, c.Entrenador.Usuario, c.Ana.Usuario })
        {
            var cliente = await ComoAsync(quien);

            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync(c.Ruta($"/pagos/{informado.Id}/rechazo"), new { motivo = "x" })).StatusCode);
        }

        var enRevision = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta("/pagos/en-revision")));
        Assert.Equal(1, enRevision.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task La_lista_en_revision_no_la_ven_el_entrenador_ni_la_familia()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        foreach (var quien in new[] { c.Entrenador.Usuario, c.Ana.Usuario })
        {
            Assert.Equal(
                HttpStatusCode.Forbidden, (await (await ComoAsync(quien)).GetAsync(c.Ruta("/pagos/en-revision"))).StatusCode);
        }
    }

    [Fact]
    public async Task Un_pago_de_otro_club_o_inexistente_responde_404()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var ajeno = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(ajeno.Ana.Integrante, new DateOnly(2026, 10, 1));
        var deOtro = await _fabrica.Pagos.CrearPagoEnRevisionAsync(ajeno.Club, ajeno.Ana.Integrante, 1000m, conSoporte: true);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        Assert.Equal(HttpStatusCode.NotFound, (await presidente.PostAsync(c.Ruta($"/pagos/{deOtro.Id}/confirmacion"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await presidente.PostAsync(c.Ruta($"/pagos/{Guid.NewGuid()}/rechazo"), new { motivo = "x" })).StatusCode);
    }
}
