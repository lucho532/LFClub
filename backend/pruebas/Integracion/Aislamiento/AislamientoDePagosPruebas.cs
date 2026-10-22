using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Aislamiento;

/// <summary>
/// Nada de un club aparece en las consultas de otro: ningún cargo, pago, medio, soporte, recibo,
/// resguardo ni total (spec 015; CE-009, RF-036), y el DESARROLLADOR no tiene ruta de club (RF-033).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AislamientoDePagosPruebas
{
    private readonly FabricaApi _fabrica;

    public AislamientoDePagosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private async Task<(ClubDePagos Club, Pago Confirmado, Pago EnRevision)> ClubConTodoAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var confirmado = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 30000m, cargo);
        var enRevision = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 20000m, conSoporte: true);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.MediosPagoClub.Add(new MedioPagoClub
            {
                ClubId = c.Club.Id, Entidad = "Nequi", Titular = "Club", Numero = "3001234567", CreadoEn = DateTime.UtcNow,
            });
            return await contexto.SaveChangesAsync();
        });
        return (c, confirmado, enRevision);
    }

    [Fact]
    public async Task Un_presidente_de_otro_club_recibe_404_en_cada_dato_de_pagos_de_este_club()
    {
        var (c, confirmado, enRevision) = await ClubConTodoAsync();
        var ajeno = await _fabrica.Pagos.CrearClubAsync();
        var intruso = await ComoAsync(ajeno.Presidente.Usuario);

        var rutas = new[]
        {
            c.Ruta("/mensualidad"),
            c.Ruta("/medios-de-pago"),
            c.Cuenta(c.Ana.Integrante),
            c.Ruta("/pagos/en-revision"),
            c.Ruta("/finanzas"),
            c.Ruta($"/pagos/{confirmado.Id}"),
            c.Ruta($"/pagos/{confirmado.Id}/resguardo"),
            c.Ruta($"/pagos/{enRevision.Id}/soporte"),
        };

        foreach (var ruta in rutas)
        {
            var respuesta = await intruso.GetAsync(ruta);

            Assert.True(respuesta.StatusCode == HttpStatusCode.NotFound, $"{ruta}: {(int)respuesta.StatusCode}");
            Assert.DoesNotContain(c.Club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Las_operaciones_que_cambian_datos_tampoco_funcionan_desde_otro_club()
    {
        var (c, confirmado, enRevision) = await ClubConTodoAsync();
        var ajeno = await _fabrica.Pagos.CrearClubAsync();
        var intruso = await ComoAsync(ajeno.Presidente.Usuario);

        var respuestas = new[]
        {
            await intruso.PutAsync(c.Ruta("/mensualidad"), new { valor = 1000 }),
            await intruso.PostAsync(c.Ruta("/medios-de-pago"), new { entidad = "X", titular = "X", numero = "1", activo = true }),
            await intruso.PostAsync(
                c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/pagos"),
                new { valor = 1000, medio = "EFECTIVO", fecha = _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd") }),
            await intruso.PostAsync(c.Ruta($"/pagos/{enRevision.Id}/confirmacion")),
            await intruso.PostAsync(c.Ruta($"/pagos/{enRevision.Id}/rechazo"), new { motivo = "x" }),
            await intruso.PostAsync(c.Ruta($"/pagos/{confirmado.Id}/anulacion"), new { motivo = "x" }),
        };

        Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode));
        var estados = await _fabrica.ConContextoAsync(async contexto => (
            await contexto.Pagos.IgnoreQueryFilters().Where(p => p.Id == confirmado.Id || p.Id == enRevision.Id)
                .Select(p => p.Estado).ToListAsync()));
        Assert.Equal(new[] { EstadoPago.CONFIRMADO, EstadoPago.EN_REVISION }.Order(), estados.Order());
    }

    [Fact]
    public async Task Cada_club_ve_solo_sus_propios_totales_medios_y_pagos_en_revision()
    {
        var (c, _, _) = await ClubConTodoAsync();
        var (otro, _, _) = await ClubConTodoAsync();
        await _fabrica.Pagos.CrearPagoEnRevisionAsync(otro.Club, otro.Beto.Integrante, 1000m);

        var delUno = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta("/pagos/en-revision")));
        var delOtro = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(otro.Presidente.Usuario)).GetAsync(otro.Ruta("/pagos/en-revision")));
        var medios = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta("/medios-de-pago")));

        Assert.Equal(1, delUno.GetProperty("total").GetInt32());
        Assert.Equal(2, delOtro.GetProperty("total").GetInt32());
        Assert.Equal(1, medios.GetArrayLength());
    }

    [Fact]
    public async Task El_desarrollador_no_tiene_ruta_a_los_datos_de_pagos_de_ningun_club()
    {
        var (c, confirmado, enRevision) = await ClubConTodoAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var rutas = new[]
        {
            c.Ruta("/finanzas"),
            c.Ruta("/pagos/en-revision"),
            c.Ruta($"/pagos/{confirmado.Id}"),
            c.Ruta($"/pagos/{confirmado.Id}/resguardo"),
            c.Ruta($"/pagos/{enRevision.Id}/soporte"),
            c.Cuenta(c.Ana.Integrante),
        };

        foreach (var ruta in rutas)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await desarrollador.GetAsync(ruta)).StatusCode);
        }

        // Y el detalle del club en el panel no trae ninguna cifra de dinero.
        var detalle = await (await desarrollador.GetAsync($"/api/plataforma/clubes/{c.Club.Id}")).Content.ReadAsStringAsync();
        foreach (var palabra in new[] { "recaudo", "saldo", "cartera", "valorMensualidad", "recibo" })
        {
            Assert.DoesNotContain(palabra, detalle, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Un_cargo_o_pago_de_un_club_no_se_ve_aunque_se_conozca_su_identificador_desde_otra_cuenta_del_club()
    {
        var (c, confirmado, enRevision) = await ClubConTodoAsync();
        var otraFamilia = await ComoAsync(c.Beto.Usuario);

        var respuestas = new[]
        {
            await otraFamilia.GetAsync(c.Ruta($"/pagos/{confirmado.Id}")),
            await otraFamilia.GetAsync(c.Ruta($"/pagos/{confirmado.Id}/resguardo")),
            await otraFamilia.GetAsync(c.Ruta($"/pagos/{enRevision.Id}/soporte")),
            await otraFamilia.GetAsync(c.Cuenta(c.Ana.Integrante)),
        };

        Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode));
    }
}
