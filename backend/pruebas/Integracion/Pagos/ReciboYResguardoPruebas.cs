using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;
using SkiaSharp;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// Cada pago confirmado tiene su recibo numerado y su resguardo en imagen, que se conservan por separado:
/// el recibo como datos y el resguardo dibujado al pedirlo (spec 015, historia 8; RF-024 y RF-025).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ReciboYResguardoPruebas
{
    private readonly FabricaApi _fabrica;

    public ReciboYResguardoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    /// <summary>Un club que le cobra a Ana un cargo y registra su pago por la API, con el recibo n.º 1.</summary>
    private async Task<(ClubDePagos Club, JsonElement Pago)> ClubConPagoRegistradoAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 11, 1));
        var respuesta = await (await ComoAsync(c.Presidente.Usuario)).PostAsync(
            c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/pagos"),
            new { valor = 90000, medio = "TRANSFERENCIA", entidad = "Nequi", fecha = _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (c, await ClienteDePrueba.LeerAsync<JsonElement>(respuesta));
    }

    private static string RutaDelResguardo(ClubDePagos c, JsonElement pago) =>
        c.Ruta($"/pagos/{pago.GetProperty("id").GetGuid()}/resguardo");

    [Fact]
    public async Task El_recibo_trae_el_club_el_jugador_el_valor_el_medio_la_fecha_quien_confirmo_los_cargos_y_el_saldo()
    {
        var (c, creado) = await ClubConPagoRegistradoAsync();

        var respuesta = await (await ComoAsync(c.Ana.Usuario)).GetAsync(c.Ruta($"/pagos/{creado.GetProperty("id").GetGuid()}"));

        var recibo = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(1, recibo.GetProperty("numeroRecibo").GetInt32());
        Assert.Equal($"{c.Ana.Integrante.Nombres} {c.Ana.Integrante.Apellidos}", recibo.GetProperty("jugadorNombre").GetString());
        Assert.Equal(90000m, recibo.GetProperty("valor").GetDecimal());
        Assert.Equal("TRANSFERENCIA", recibo.GetProperty("medio").GetString());
        Assert.Equal("Nequi", recibo.GetProperty("entidad").GetString());
        Assert.Equal($"{c.Presidente.Integrante.Nombres} {c.Presidente.Integrante.Apellidos}", recibo.GetProperty("confirmadoPorNombre").GetString());
        Assert.Equal("Mensualidad octubre de 2026; Mensualidad noviembre de 2026", recibo.GetProperty("concepto").GetString());
        Assert.Equal(40000m, recibo.GetProperty("saldoTrasPago").GetDecimal());
        Assert.DoesNotContain(c.Ana.Integrante.NumeroDocumento, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_resguardo_es_un_png_de_1080_por_1350_con_cabeceras_de_privacidad()
    {
        var (c, creado) = await ClubConPagoRegistradoAsync();

        foreach (var quien in new[] { c.Presidente.Usuario, c.Directivo.Usuario, c.Ana.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).GetAsync(RutaDelResguardo(c, creado));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("image/png", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.True(respuesta.Headers.CacheControl?.Private);
            Assert.True(respuesta.Headers.CacheControl?.NoStore);
            Assert.Equal("nosniff", Assert.Single(respuesta.Headers.GetValues("X-Content-Type-Options")));
            using var imagen = SKBitmap.Decode(await respuesta.Content.ReadAsByteArrayAsync());
            Assert.Equal(1080, imagen.Width);
            Assert.Equal(1350, imagen.Height);
        }
    }

    [Fact]
    public async Task El_resguardo_se_dibuja_al_pedirlo_y_no_es_igual_para_dos_pagos_distintos()
    {
        var (c, creado) = await ClubConPagoRegistradoAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);
        var segundo = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.PostAsync(
            c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/pagos"),
            new { valor = 10000, medio = "EFECTIVO", fecha = _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd") }));

        var uno = await (await presidente.GetAsync(RutaDelResguardo(c, creado))).Content.ReadAsByteArrayAsync();
        var otro = await (await presidente.GetAsync(RutaDelResguardo(c, segundo))).Content.ReadAsByteArrayAsync();

        Assert.NotEqual(uno, otro);
        Assert.Equal(2, segundo.GetProperty("numeroRecibo").GetInt32());
    }

    [Fact]
    public async Task Un_pago_en_revision_o_rechazado_no_tiene_recibo_ni_resguardo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var enRevision = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 30000m, conSoporte: true);
        var rechazado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 30000m);
        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(c.Ruta($"/pagos/{rechazado.Id}/rechazo"), new { motivo = "No" });

        foreach (var pago in new[] { enRevision, rechazado })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await presidente.GetAsync(c.Ruta($"/pagos/{pago.Id}/resguardo"))).StatusCode);
        }

        var cuenta = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(c.Cuenta(c.Ana.Integrante)));
        Assert.All(cuenta.GetProperty("pagos").EnumerateArray(), pago =>
        {
            Assert.False(pago.GetProperty("tieneResguardo").GetBoolean());
            Assert.Equal(JsonValueKind.Null, pago.GetProperty("numeroRecibo").ValueKind);
        });
    }

    [Fact]
    public async Task Un_pago_anulado_conserva_recibo_y_resguardo_y_el_resguardo_muestra_el_sello()
    {
        var (c, creado) = await ClubConPagoRegistradoAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);
        var antes = await (await presidente.GetAsync(RutaDelResguardo(c, creado))).Content.ReadAsByteArrayAsync();

        await presidente.PostAsync(c.Ruta($"/pagos/{creado.GetProperty("id").GetGuid()}/anulacion"), new { motivo = "Error" });

        var respuesta = await presidente.GetAsync(RutaDelResguardo(c, creado));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var despues = await respuesta.Content.ReadAsByteArrayAsync();
        Assert.NotEqual(antes, despues);
        var recibo = await ClienteDePrueba.LeerAsync<JsonElement>(
            await presidente.GetAsync(c.Ruta($"/pagos/{creado.GetProperty("id").GetGuid()}")));
        Assert.Equal(1, recibo.GetProperty("numeroRecibo").GetInt32());
        Assert.True(recibo.GetProperty("tieneResguardo").GetBoolean());
    }

    [Fact]
    public async Task Otra_familia_u_otro_club_no_los_ven_y_el_entrenador_recibe_403()
    {
        var (c, creado) = await ClubConPagoRegistradoAsync();
        var ajeno = await _fabrica.Pagos.CrearClubAsync();
        var idDelPago = creado.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await (await ComoAsync(c.Beto.Usuario)).GetAsync(RutaDelResguardo(c, creado))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await (await ComoAsync(c.Beto.Usuario)).GetAsync(c.Ruta($"/pagos/{idDelPago}"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await (await ComoAsync(ajeno.Presidente.Usuario)).GetAsync(c.Ruta($"/pagos/{idDelPago}/resguardo"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await ComoAsync(c.Entrenador.Usuario)).GetAsync(RutaDelResguardo(c, creado))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await ComoAsync(c.Entrenador.Usuario)).GetAsync(c.Ruta($"/pagos/{idDelPago}"))).StatusCode);
    }

    [Fact]
    public async Task Tras_eliminar_la_cuenta_el_recibo_se_conserva_y_el_resguardo_deja_de_existir()
    {
        var (c, creado) = await ClubConPagoRegistradoAsync();
        var idDelPago = creado.GetProperty("id").GetGuid();
        await (await ComoAsync(c.Ana.Usuario)).PostAsync("/api/cuenta/eliminacion", new { contrasena = Sembrador.Contrasena });
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var recibo = await presidente.GetAsync(c.Ruta($"/pagos/{idDelPago}"));
        var resguardo = await presidente.GetAsync(c.Ruta($"/pagos/{idDelPago}/resguardo"));

        Assert.Equal(HttpStatusCode.OK, recibo.StatusCode);
        var datos = await ClienteDePrueba.LeerAsync<JsonElement>(recibo);
        Assert.Equal(1, datos.GetProperty("numeroRecibo").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, resguardo.StatusCode);
    }
}
