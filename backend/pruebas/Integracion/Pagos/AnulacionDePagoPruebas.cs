using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// Un pago confirmado no se edita ni se borra: se anula con quién, cuándo y por qué, y el saldo se
/// recalcula (spec 015, historia 7; RF-018 y RF-019; constitución §16.3).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AnulacionDePagoPruebas
{
    private readonly FabricaApi _fabrica;

    public AnulacionDePagoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private async Task<(ClubDePagos Club, Cargo Cargo, Pago Pago)> ClubConPagoConfirmadoAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        return (c, cargo, pago);
    }

    private static string Ruta(ClubDePagos c, Pago pago) => c.Ruta($"/pagos/{pago.Id}/anulacion");

    [Fact]
    public async Task Anular_con_motivo_deja_autor_fecha_y_motivo_y_el_saldo_vuelve_a_subir()
    {
        var (c, cargo, pago) = await ClubConPagoConfirmadoAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(Ruta(c, pago), new { motivo = "Me equivoqué de jugador" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var anulado = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("ANULADO", anulado.GetProperty("estado").GetString());
        Assert.Equal("Me equivoqué de jugador", anulado.GetProperty("motivoAnulacion").GetString());
        Assert.Equal($"{c.Presidente.Integrante.Nombres} {c.Presidente.Integrante.Apellidos}", anulado.GetProperty("anuladoPorNombre").GetString());
        Assert.Equal(JsonValueKind.String, anulado.GetProperty("anuladoEn").ValueKind);
        Assert.Equal(0m, await _fabrica.Pagos.PagadoAsync(cargo));

        var cuenta = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(c.Cuenta(c.Ana.Integrante)));
        Assert.Equal(65000m, cuenta.GetProperty("saldo").GetDecimal());
        Assert.Equal("DEBE", cuenta.GetProperty("estado").GetString());
        var enHistorial = Assert.Single(cuenta.GetProperty("pagos").EnumerateArray());
        Assert.Equal("ANULADO", enHistorial.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Un_pago_anulado_conserva_su_recibo_y_su_resguardo_y_se_puede_pagar_de_nuevo()
    {
        var (c, _, pago) = await ClubConPagoConfirmadoAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(Ruta(c, pago), new { motivo = "Error" });

        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(c.Ruta($"/pagos/{pago.Id}")));
        var nuevo = await presidente.PostAsync(
            c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/pagos"),
            new { valor = 65000, medio = "EFECTIVO", fecha = _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd") });

        Assert.Equal(pago.NumeroRecibo, detalle.GetProperty("numeroRecibo").GetInt32());
        Assert.True(detalle.GetProperty("tieneResguardo").GetBoolean());
        Assert.Equal(HttpStatusCode.Created, nuevo.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Sin_motivo_se_rechaza_y_el_pago_sigue_confirmado(string? motivo)
    {
        var (c, cargo, pago) = await ClubConPagoConfirmadoAsync();

        var respuesta = await (await ComoAsync(c.Presidente.Usuario)).PostAsync(Ruta(c, pago), new { motivo });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task Anular_dos_veces_se_rechaza()
    {
        var (c, _, pago) = await ClubConPagoConfirmadoAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(Ruta(c, pago), new { motivo = "Error" });

        var otraVez = await presidente.PostAsync(Ruta(c, pago), new { motivo = "Otra vez" });

        Assert.Equal(HttpStatusCode.Conflict, otraVez.StatusCode);
        Assert.Equal("pago_no_esta_confirmado", await ClienteDePrueba.CodigoAsync(otraVez));
    }

    [Fact]
    public async Task Un_pago_en_revision_o_rechazado_no_se_anula()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var enRevision = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        var rechazado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m);
        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(c.Ruta($"/pagos/{rechazado.Id}/rechazo"), new { motivo = "No" });

        foreach (var pago in new[] { enRevision, rechazado })
        {
            var respuesta = await presidente.PostAsync(Ruta(c, pago), new { motivo = "x" });

            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal("pago_no_esta_confirmado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task El_directivo_el_entrenador_y_la_familia_reciben_403()
    {
        var (c, cargo, pago) = await ClubConPagoConfirmadoAsync();

        foreach (var quien in new[] { c.Directivo.Usuario, c.Entrenador.Usuario, c.Ana.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).PostAsync(Ruta(c, pago), new { motivo = "x" });

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        }

        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task Un_pago_de_otro_club_responde_404()
    {
        var (c, _, _) = await ClubConPagoConfirmadoAsync();
        var (_, _, ajeno) = await ClubConPagoConfirmadoAsync();

        var respuesta = await (await ComoAsync(c.Presidente.Usuario)).PostAsync(
            c.Ruta($"/pagos/{ajeno.Id}/anulacion"), new { motivo = "x" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_pago_anulado_de_una_transferencia_conserva_su_soporte()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion"));

        await presidente.PostAsync(Ruta(c, informado), new { motivo = "Era de otro" });

        var soporte = await (await ComoAsync(c.Ana.Usuario)).GetAsync(c.Ruta($"/pagos/{informado.Id}/soporte"));
        Assert.Equal(HttpStatusCode.OK, soporte.StatusCode);
    }

    [Fact]
    public async Task No_existe_ruta_para_editar_o_borrar_un_pago_confirmado()
    {
        var (c, _, pago) = await ClubConPagoConfirmadoAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);
        var ruta = c.Ruta($"/pagos/{pago.Id}");

        foreach (var respuesta in new[]
        {
            await presidente.PutAsync(ruta, new { valor = 1 }),
            await presidente.DeleteAsync(ruta),
            await presidente.Http.PatchAsync(ruta, JsonContent.Create(new { valor = 1 })),
        })
        {
            Assert.Contains(respuesta.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        }
    }

    [Fact]
    public async Task La_anulacion_no_cuenta_en_el_recaudo_y_el_estado_de_la_lista_se_actualiza()
    {
        var (c, _, pago) = await ClubConPagoConfirmadoAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(Ruta(c, pago), new { motivo = "Error" });

        var lista = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync(c.Ruta("/jugadores")));

        var ana = lista.EnumerateArray().SelectMany(g => g.GetProperty("jugadores").EnumerateArray())
            .Single(j => j.GetProperty("usuarioRolId").GetGuid() == c.Ana.Integrante.Id);
        Assert.Equal("DEBE", ana.GetProperty("estadoCuenta").GetString());
    }
}
