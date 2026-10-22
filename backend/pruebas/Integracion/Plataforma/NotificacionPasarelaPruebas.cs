using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Base.SembradorCobrosPlataforma;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>La notificación de la pasarela es la única que marca pagado un cobro (constitución §7.4, spec 017, historia 2): la firma, el valor, la referencia y el estado de la transacción.</summary>
[Collection(ColeccionApi.Nombre)]
public class NotificacionPasarelaPruebas : NotificacionPasarelaBase
{
    public NotificacionPasarelaPruebas(FabricaApi fabrica)
        : base(fabrica)
    {
    }

    [Fact]
    public async Task Una_notificacion_aprobada_cuya_referencia_y_valor_coinciden_marca_el_cobro_pagado()
    {
        var (_, cobro, intento) = await EscenarioAsync();

        var respuesta = await EnviarAsync(intento.Referencia, 45000, transaccion: "tx-aprobada-1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(EstadoCobroPlataforma.PAGADO, (await CobroAsync(cobro)).Estado);
        var pago = Assert.Single(await PagosAsync(cobro));
        Assert.Equal(45000m, pago.Valor);
        Assert.Equal(OrigenPagoPlataforma.PASARELA, pago.Origen);
        Assert.Equal(intento.Referencia, pago.Referencia);
        Assert.Equal("tx-aprobada-1", pago.TransaccionId);
        Assert.Equal("PSE", pago.MedioDePago);
        Assert.True(pago.PagadoEn > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(ResultadoNotificacion.APLICADA, Assert.Single(await NotificacionesAsync(cobro)).Resultado);
    }

    [Fact]
    public async Task Una_firma_alterada_responde_401_no_cambia_el_cobro_y_deja_una_fila_sin_datos()
    {
        var (_, cobro, intento) = await EscenarioAsync();
        var (cuerpo, checksum) = Notificacion(intento.Referencia, 45000);
        var alterado = cuerpo.Replace("4500000", "100");
        var antes = await _fabrica.ConContextoAsync(c => c.NotificacionesPasarela.CountAsync(n => n.Resultado == ResultadoNotificacion.FIRMA_INVALIDA));

        var respuesta = await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(alterado, checksum);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("firma_invalida", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await CobroAsync(cobro)).Estado);
        Assert.Empty(await PagosAsync(cobro));
        var despues = await _fabrica.ConContextoAsync(c => c.NotificacionesPasarela.AsNoTracking()
            .Where(n => n.Resultado == ResultadoNotificacion.FIRMA_INVALIDA).ToListAsync());
        Assert.Equal(antes + 1, despues.Count);
        Assert.All(despues, n => Assert.Null(n.TransaccionId));
        Assert.All(despues, n => Assert.Null(n.Referencia));
    }

    [Fact]
    public async Task Un_checksum_de_la_cabecera_distinto_o_una_notificacion_sin_firma_responden_401()
    {
        var (_, cobro, intento) = await EscenarioAsync();
        var (cuerpo, _) = Notificacion(intento.Referencia, 45000);

        var distinto = await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, new string('a', 64));
        var sinFirma = await _fabrica.CobrosPlataforma.EnviarNotificacionAsync("{\"event\":\"transaction.updated\"}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, distinto.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sinFirma.StatusCode);
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await CobroAsync(cobro)).Estado);
    }

    [Fact]
    public async Task Sin_la_pasarela_configurada_toda_notificacion_responde_401()
    {
        var (_, cobro, intento) = await EscenarioAsync();
        using var _ = _fabrica.SinPasarela();

        var respuesta = await EnviarAsync(intento.Referencia, 45000);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await CobroAsync(cobro)).Estado);
    }

    [Fact]
    public async Task Un_valor_distinto_responde_200_y_no_cambia_el_cobro()
    {
        var (_, cobro, intento) = await EscenarioAsync(total: 45000);

        var respuesta = await EnviarAsync(intento.Referencia, 44000);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await CobroAsync(cobro)).Estado);
        Assert.Empty(await PagosAsync(cobro));
        Assert.Equal(ResultadoNotificacion.VALOR_NO_COINCIDE, Assert.Single(await NotificacionesAsync(cobro)).Resultado);
    }

    [Fact]
    public async Task Una_referencia_desconocida_responde_200_y_queda_registrada()
    {
        var referencia = "LFC-" + Guid.NewGuid().ToString("N");

        var respuesta = await EnviarAsync(referencia, 45000);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var fila = await _fabrica.ConContextoAsync(c => c.NotificacionesPasarela.AsNoTracking().SingleAsync(n => n.Referencia == referencia));
        Assert.Equal(ResultadoNotificacion.REFERENCIA_DESCONOCIDA, fila.Resultado);
        Assert.Null(fila.CobroId);
    }

    [Theory]
    [InlineData("DECLINED")]
    [InlineData("VOIDED")]
    [InlineData("ERROR")]
    public async Task Un_pago_no_aprobado_deja_el_cobro_pendiente_y_se_puede_iniciar_otro_intento(string estado)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var intento = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);

        var respuesta = await EnviarAsync(intento.Referencia, 50000, estado);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await CobroAsync(cobro)).Estado);
        Assert.Equal(ResultadoNotificacion.NO_APROBADA, Assert.Single(await NotificacionesAsync(cobro)).Resultado);
        var otro = await presidente.PostAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}/intentos");
        Assert.Equal(HttpStatusCode.Created, otro.StatusCode);
    }

    [Fact]
    public async Task Tras_un_pago_rechazado_el_presidente_lo_ve_y_deja_de_figurar_en_verificacion_hasta_el_siguiente_intento()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var intento = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var ruta = $"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}";

        var antes = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(ruta));
        await EnviarAsync(intento.Referencia, 50000, "DECLINED");
        var rechazado = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(ruta));
        await presidente.PostAsync($"{ruta}/intentos");
        var otroIntento = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(ruta));

        Assert.True(antes.GetProperty("enVerificacion").GetBoolean());
        Assert.False(antes.GetProperty("ultimoPagoNoAprobado").GetBoolean());
        Assert.False(rechazado.GetProperty("enVerificacion").GetBoolean());
        Assert.True(rechazado.GetProperty("ultimoPagoNoAprobado").GetBoolean());
        Assert.Equal("PENDIENTE", rechazado.GetProperty("estado").GetString());
        Assert.True(otroIntento.GetProperty("enVerificacion").GetBoolean());
        Assert.False(otroIntento.GetProperty("ultimoPagoNoAprobado").GetBoolean());
    }
}
