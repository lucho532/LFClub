using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// Quién ve un pago y su recibo, y que un pago confirmado no se edita ni se borra (spec 015, historia 4;
/// RF-018, RF-024, RF-025 y RF-035; constitución §16.3).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConsultaDePagoPruebas
{
    private readonly FabricaApi _fabrica;

    public ConsultaDePagoPruebas(FabricaApi fabrica)
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

    private static string RutaPagos(ClubDePagos c, UsuarioRol jugador) => c.Ruta($"/jugadores/{jugador.Id}/pagos");

    private string Hoy => _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd");

    private object Cuerpo(decimal valor = 65000m, string medio = "EFECTIVO", string? entidad = null) =>
        new { valor, medio, entidad, referencia = (string?)null, fecha = Hoy, cargoIds = (Guid[]?)null };

    [Fact]
    public async Task El_pago_guarda_quien_cuando_medio_entidad_y_cargos_cubiertos_y_lo_ven_el_equipo_y_la_familia()
    {
        var (c, _, _) = await ClubConDosCargosAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);
        var creado = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.PostAsync(
            RutaPagos(c, c.Ana.Integrante), Cuerpo(valor: 130000m, medio: "TRANSFERENCIA", entidad: "Bancolombia")));
        var ruta = c.Ruta($"/pagos/{creado.GetProperty("id").GetGuid()}");

        foreach (var quien in new[] { c.Presidente.Usuario, c.Directivo.Usuario, c.Ana.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).GetAsync(ruta);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var pago = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
            Assert.Equal("Bancolombia", pago.GetProperty("entidad").GetString());
            Assert.Equal(c.Presidente.Integrante.Nombres + " " + c.Presidente.Integrante.Apellidos, pago.GetProperty("creadoPorNombre").GetString());
            Assert.Equal(JsonValueKind.String, pago.GetProperty("confirmadoEn").ValueKind);
            Assert.Contains("septiembre", pago.GetProperty("concepto").GetString());
            Assert.Contains("octubre", pago.GetProperty("concepto").GetString());
            Assert.DoesNotContain(c.Ana.Integrante.NumeroDocumento, await respuesta.Content.ReadAsStringAsync());
        }

        Assert.Equal(HttpStatusCode.NotFound, (await (await ComoAsync(c.Beto.Usuario)).GetAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await ComoAsync(c.Entrenador.Usuario)).GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task No_existe_ninguna_operacion_para_editar_o_borrar_un_pago_confirmado()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        var presidente = await ComoAsync(c.Presidente.Usuario);
        var ruta = c.Ruta($"/pagos/{pago.Id}");

        Assert.NotEqual(HttpStatusCode.OK, (await presidente.PutAsync(ruta, new { valor = 1000 })).StatusCode);
        Assert.NotEqual(HttpStatusCode.NoContent, (await presidente.DeleteAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await presidente.GetAsync(ruta)).StatusCode);
    }
}
