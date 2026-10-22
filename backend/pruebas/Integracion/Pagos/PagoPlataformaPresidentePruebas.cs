using System.Net;
using System.Text.Json;
using System.Web;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>Lo que ve y hace el PRESIDENTE con el cobro de la plataforma (spec 017, historia 1).</summary>
[Collection(ColeccionApi.Nombre)]
public class PagoPlataformaPresidentePruebas
{
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private readonly FabricaApi _fabrica;

    public PagoPlataformaPresidentePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private async Task<(Club Club, CobroPlataforma Cobro, ClienteDePrueba Presidente)> EscenarioAsync(
        Rol rol = Rol.PRESIDENTE, decimal total = 45000, EstadoCobroPlataforma estado = EstadoCobroPlataforma.PENDIENTE)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(
            club, Noviembre, new DateOnly(2026, 11, 8), total, estado);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        return (club, cobro, cliente);
    }

    private static string Ruta(Club club) => $"/api/clubes/{club.Id}/pago-plataforma";

    [Fact]
    public async Task El_presidente_ve_los_pendientes_con_el_detalle_por_rol_y_el_total()
    {
        var (club, cobro, presidente) = await EscenarioAsync();

        var respuesta = await presidente.GetAsync(Ruta(club));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no-store", respuesta.Headers.CacheControl?.ToString());
        var resumen = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.True(resumen.GetProperty("pagoEnLineaDisponible").GetBoolean());
        Assert.False(resumen.GetProperty("suspendidoPorImpago").GetBoolean());
        Assert.Equal(45000, resumen.GetProperty("totalPendiente").GetDecimal());
        var pendiente = Assert.Single(resumen.GetProperty("pendientes").EnumerateArray());
        Assert.Equal(cobro.Id, pendiente.GetProperty("cobroId").GetGuid());
        Assert.Equal("2026-11", pendiente.GetProperty("periodo").GetString());
        Assert.Equal("2026-11-08", pendiente.GetProperty("suspendeDesde").GetString());
        Assert.Equal(4, pendiente.GetProperty("detalle").GetArrayLength());
        var jugadores = pendiente.GetProperty("detalle").EnumerateArray().Single(l => l.GetProperty("rol").GetString() == "JUGADOR");
        Assert.Equal(10, jugadores.GetProperty("integrantes").GetInt32());
        Assert.Equal(45000, jugadores.GetProperty("subtotal").GetDecimal());
        Assert.Empty(resumen.GetProperty("historial").EnumerateArray());
    }

    [Fact]
    public async Task El_historial_trae_los_cobros_pagados_y_anulados()
    {
        var (club, _, presidente) = await EscenarioAsync();
        var pagado = await _fabrica.CobrosPlataforma.CrearCobroAsync(
            club, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8), 40000, EstadoCobroPlataforma.PAGADO);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.PagosPlataforma.Add(new PagoPlataforma
            {
                CobroId = pagado.Id, Valor = 40000, PagadoEn = DateTime.UtcNow, Origen = OrigenPagoPlataforma.PASARELA,
                Referencia = "LFC-x", TransaccionId = "tx-1", MedioDePago = "PSE",
            });
            await contexto.SaveChangesAsync();
            return true;
        });
        await _fabrica.CobrosPlataforma.CrearCobroAsync(
            club, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 8), 30000, EstadoCobroPlataforma.ANULADO);

        var resumen = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(Ruta(club)));

        var historial = resumen.GetProperty("historial").EnumerateArray().ToList();
        Assert.Equal(["2026-10", "2026-09"], historial.Select(c => c.GetProperty("periodo").GetString()).ToList());
        Assert.Equal("PASARELA", historial[0].GetProperty("origenDelPago").GetString());
        Assert.Equal("PSE", historial[0].GetProperty("medioDePago").GetString());
        Assert.Equal("LFC-x", historial[0].GetProperty("referencia").GetString());
    }

    [Fact]
    public async Task Iniciar_el_pago_devuelve_la_direccion_con_el_valor_y_la_referencia_y_no_cambia_el_cobro()
    {
        var (club, cobro, presidente) = await EscenarioAsync(total: 45000);

        var respuesta = await presidente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var intento = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        var consulta = HttpUtility.ParseQueryString(new Uri(intento.GetProperty("urlDePago").GetString()!).Query);
        Assert.Equal("4500000", consulta["amount-in-cents"]);
        Assert.Equal(intento.GetProperty("referencia").GetString(), consulta["reference"]);
        Assert.StartsWith("LFC-", consulta["reference"]);
        Assert.Equal(
            $"http://localhost:5173/club/{club.Id}/pago-plataforma/retorno?cobro={cobro.Id}", consulta["redirect-url"]);

        var guardado = await _fabrica.ConContextoAsync(contexto =>
            contexto.IntentosPagoPlataforma.AsNoTracking().SingleAsync(i => i.Referencia == consulta["reference"]));
        Assert.Equal(cobro.Id, guardado.CobroId);
        Assert.Equal(45000m, guardado.Valor);
        var despues = (await _fabrica.CobrosPlataforma.CobrosDeAsync(club)).Single();
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, despues.Estado);
    }

    [Fact]
    public async Task Dos_intentos_seguidos_crean_dos_referencias_distintas()
    {
        var (club, cobro, presidente) = await EscenarioAsync();

        var primero = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos"));
        var segundo = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos"));

        Assert.NotEqual(primero.GetProperty("referencia").GetString(), segundo.GetProperty("referencia").GetString());
    }

    [Fact]
    public async Task El_valor_del_intento_sale_del_cobro_aunque_el_cliente_mande_otro()
    {
        var (club, cobro, presidente) = await EscenarioAsync(total: 45000);

        var respuesta = await presidente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos", new { valor = 1, total = 1 });

        var consulta = HttpUtility.ParseQueryString(
            new Uri((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("urlDePago").GetString()!).Query);
        Assert.Equal("4500000", consulta["amount-in-cents"]);
    }

    [Theory]
    [InlineData(EstadoCobroPlataforma.PAGADO)]
    [InlineData(EstadoCobroPlataforma.ANULADO)]
    public async Task Un_cobro_pagado_o_anulado_no_se_puede_pagar(EstadoCobroPlataforma estado)
    {
        var (club, cobro, presidente) = await EscenarioAsync(estado: estado);

        var respuesta = await presidente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("cobro_no_pendiente", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Sin_la_pasarela_configurada_el_resumen_lo_dice_y_el_intento_responde_503()
    {
        var (club, cobro, presidente) = await EscenarioAsync();
        using var _ = _fabrica.SinPasarela();

        var resumen = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(Ruta(club)));
        var intento = await presidente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos");

        Assert.False(resumen.GetProperty("pagoEnLineaDisponible").GetBoolean());
        Assert.Single(resumen.GetProperty("pendientes").EnumerateArray());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, intento.StatusCode);
        Assert.Equal("pago_no_disponible", await ClienteDePrueba.CodigoAsync(intento));
    }

    [Fact]
    public async Task Un_cobro_con_un_intento_reciente_figura_en_verificacion()
    {
        var (club, cobro, presidente) = await EscenarioAsync();
        await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);

        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync($"{Ruta(club)}/cobros/{cobro.Id}"));

        Assert.True(detalle.GetProperty("enVerificacion").GetBoolean());
        Assert.Equal("PENDIENTE", detalle.GetProperty("estado").GetString());
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public async Task Los_demas_roles_reciben_403_en_las_tres_operaciones(Rol rol)
    {
        var (club, cobro, cliente) = await EscenarioAsync(rol);

        foreach (var respuesta in new[]
        {
            await cliente.GetAsync(Ruta(club)),
            await cliente.GetAsync($"{Ruta(club)}/cobros/{cobro.Id}"),
            await cliente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos"),
        })
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task Sin_sesion_las_tres_operaciones_responden_401()
    {
        var (club, cobro, _) = await EscenarioAsync();
        var anonimo = _fabrica.CrearClienteDePrueba();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Ruta(club))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync($"{Ruta(club)}/cobros/{cobro.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos")).StatusCode);
    }

    [Fact]
    public async Task El_presidente_de_un_club_suspendido_sigue_viendo_y_pudiendo_pagar()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, new DateOnly(2026, 11, 8));
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        Assert.Equal(HttpStatusCode.OK, (await presidente.GetAsync(Ruta(club))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await presidente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos")).StatusCode);
    }

    [Fact]
    public async Task Las_respuestas_no_traen_llaves_ni_secretos_de_la_pasarela()
    {
        var (club, cobro, presidente) = await EscenarioAsync();

        var textos = new[]
        {
            await (await presidente.GetAsync(Ruta(club))).Content.ReadAsStringAsync(),
            await (await presidente.PostAsync($"{Ruta(club)}/cobros/{cobro.Id}/intentos")).Content.ReadAsStringAsync(),
        };

        foreach (var texto in textos)
        {
            Assert.DoesNotContain(SembradorCobrosPlataforma.SecretoDeIntegridad, texto);
            Assert.DoesNotContain(SembradorCobrosPlataforma.SecretoDeEventos, texto);
        }
    }
}
