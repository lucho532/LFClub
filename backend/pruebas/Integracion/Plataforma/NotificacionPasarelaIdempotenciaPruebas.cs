using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Base.SembradorCobrosPlataforma;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>La notificación de la pasarela es idempotente y a prueba de desorden (spec 017, RF-012): repeticiones, pagos duplicados, cobros anulados y el retorno del navegador.</summary>
[Collection(ColeccionApi.Nombre)]
public class NotificacionPasarelaIdempotenciaPruebas : NotificacionPasarelaBase
{
    public NotificacionPasarelaIdempotenciaPruebas(FabricaApi fabrica)
        : base(fabrica)
    {
    }

    [Fact]
    public async Task La_misma_notificacion_dos_veces_deja_un_solo_pago()
    {
        var (_, cobro, intento) = await EscenarioAsync();
        var (cuerpo, checksum) = Notificacion(intento.Referencia, 45000, transaccionId: "tx-repetida");

        var primera = await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum);
        var segunda = await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum);

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Single(await PagosAsync(cobro));
        var resultados = (await NotificacionesAsync(cobro)).Select(n => n.Resultado).ToList();
        Assert.Equal([ResultadoNotificacion.APLICADA, ResultadoNotificacion.REPETIDA], resultados);
    }

    [Fact]
    public async Task La_misma_notificacion_en_paralelo_deja_un_solo_pago()
    {
        var (_, cobro, intento) = await EscenarioAsync();
        var (cuerpo, checksum) = Notificacion(intento.Referencia, 45000, transaccionId: "tx-paralela");

        var respuestas = await Task.WhenAll(
            _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum),
            _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum),
            _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum));

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Single(await PagosAsync(cobro));
        Assert.Equal(EstadoCobroPlataforma.PAGADO, (await CobroAsync(cobro)).Estado);
    }

    [Fact]
    public async Task Un_segundo_pago_aprobado_con_otro_intento_queda_como_duplicado_sin_tocar_nada()
    {
        var (_, cobro, intento) = await EscenarioAsync();
        var otroIntento = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);
        await EnviarAsync(intento.Referencia, 45000, transaccion: "tx-uno");

        var respuesta = await EnviarAsync(otroIntento.Referencia, 45000, transaccion: "tx-dos");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var pago = Assert.Single(await PagosAsync(cobro));
        Assert.Equal("tx-uno", pago.TransaccionId);
        var resultados = (await NotificacionesAsync(cobro)).Select(n => n.Resultado).ToList();
        Assert.Equal([ResultadoNotificacion.APLICADA, ResultadoNotificacion.PAGO_DUPLICADO], resultados);
    }

    [Fact]
    public async Task Un_pago_a_un_cobro_anulado_queda_registrado_y_no_se_aplica()
    {
        var (_, cobro, intento) = await EscenarioAsync(estado: EstadoCobroPlataforma.ANULADO);

        var respuesta = await EnviarAsync(intento.Referencia, 45000);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(EstadoCobroPlataforma.ANULADO, (await CobroAsync(cobro)).Estado);
        Assert.Empty(await PagosAsync(cobro));
        Assert.Equal(ResultadoNotificacion.PAGO_A_COBRO_ANULADO, Assert.Single(await NotificacionesAsync(cobro)).Resultado);
    }

    [Fact]
    public async Task Una_notificacion_declinada_posterior_a_una_aprobada_no_revierte_el_pago()
    {
        var (_, cobro, intento) = await EscenarioAsync();
        await EnviarAsync(intento.Referencia, 45000, "APPROVED", "tx-a");

        await EnviarAsync(intento.Referencia, 45000, "DECLINED", "tx-a");

        Assert.Equal(EstadoCobroPlataforma.PAGADO, (await CobroAsync(cobro)).Estado);
        Assert.Single(await PagosAsync(cobro));
    }

    [Fact]
    public async Task Volver_del_navegador_con_parametros_de_aprobado_sin_notificacion_no_cambia_nada()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var retorno = await presidente.GetAsync(
            $"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}?id=tx-1&status=APPROVED&transactionStatus=APPROVED");

        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(retorno);
        Assert.Equal("PENDIENTE", detalle.GetProperty("estado").GetString());
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await CobroAsync(cobro)).Estado);
        Assert.Empty(await PagosAsync(cobro));
    }

    [Fact]
    public async Task Si_la_notificacion_llega_antes_de_que_el_presidente_consulte_ya_lo_ve_pagado()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var intento = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        await EnviarAsync(intento.Referencia, 50000);

        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(
            await presidente.GetAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}"));

        Assert.Equal("PAGADO", detalle.GetProperty("estado").GetString());
        Assert.Equal("PASARELA", detalle.GetProperty("origenDelPago").GetString());
        Assert.Equal(intento.Referencia, detalle.GetProperty("referencia").GetString());
        Assert.False(detalle.GetProperty("enVerificacion").GetBoolean());
    }

    [Fact]
    public async Task La_fila_guardada_no_contiene_el_cuerpo_ni_la_firma()
    {
        var (_, cobro, intento) = await EscenarioAsync();
        var (cuerpo, checksum) = Notificacion(intento.Referencia, 45000);
        await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum);

        var fila = Assert.Single(await NotificacionesAsync(cobro));
        var texto = JsonSerializer.Serialize(fila);

        Assert.DoesNotContain(checksum, texto);
        Assert.DoesNotContain("signature", texto);
        Assert.DoesNotContain("transaction.updated", texto);
        Assert.DoesNotContain(SecretoDeEventos, texto);
    }
}
