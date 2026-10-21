using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// La imagen de soporte es privada: solo la familia del jugador, el PRESIDENTE y el DIRECTIVO la abren,
/// sin caché y sin dirección pública, y se borra con la cuenta del jugador (spec 015, historia 6; RF-030).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SoporteDePagoPruebas
{
    private readonly FabricaApi _fabrica;

    public SoporteDePagoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private async Task<(ClubDePagos Club, Pago Pago)> ClubConSoporteAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var pago = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);
        return (c, pago);
    }

    [Fact]
    public async Task La_familia_el_presidente_y_el_directivo_abren_la_imagen_con_cabeceras_de_privacidad()
    {
        var (c, pago) = await ClubConSoporteAsync();

        foreach (var quien in new[] { c.Ana.Usuario, c.Presidente.Usuario, c.Directivo.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte"));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("image/png", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.True(respuesta.Headers.CacheControl?.Private);
            Assert.True(respuesta.Headers.CacheControl?.NoStore);
            Assert.Equal("nosniff", Assert.Single(respuesta.Headers.GetValues("X-Content-Type-Options")));
            Assert.Equal(Imagenes.Png(), await respuesta.Content.ReadAsByteArrayAsync());
        }
    }

    [Fact]
    public async Task Otra_familia_otro_club_y_el_entrenador_no_abren_el_soporte()
    {
        var (c, pago) = await ClubConSoporteAsync();
        var ajeno = await _fabrica.Pagos.CrearClubAsync();

        var otraFamilia = await (await ComoAsync(c.Beto.Usuario)).GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte"));
        var otroClub = await (await ComoAsync(ajeno.Presidente.Usuario)).GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte"));
        var entrenador = await (await ComoAsync(c.Entrenador.Usuario)).GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte"));
        var sinSesion = await _fabrica.CrearClienteDePrueba().GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte"));

        Assert.Equal(HttpStatusCode.NotFound, otraFamilia.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otroClub.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, entrenador.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sinSesion.StatusCode);
    }

    [Fact]
    public async Task Un_pago_sin_soporte_responde_404()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);

        var respuesta = await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte"));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_soporte_no_se_incluye_en_la_respuesta_del_pago_ni_tiene_direccion_publica()
    {
        var (c, pago) = await ClubConSoporteAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var detalle = await (await presidente.GetAsync(c.Ruta($"/pagos/{pago.Id}"))).Content.ReadAsStringAsync();
        var publica = await _fabrica.CrearClienteDePrueba().GetAsync($"/api/publico/pagos/{pago.Id}/soporte");

        Assert.Contains("\"tieneSoporte\":true", detalle);
        Assert.DoesNotContain("contenido", detalle, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(HttpStatusCode.OK, publica.StatusCode);
    }

    [Fact]
    public async Task El_soporte_se_conserva_aunque_el_pago_se_rechace_o_se_anule()
    {
        var (c, pago) = await ClubConSoporteAsync();
        await (await ComoAsync(c.Presidente.Usuario)).PostAsync(c.Ruta($"/pagos/{pago.Id}/rechazo"), new { motivo = "No coincide" });

        var respuesta = await (await ComoAsync(c.Ana.Usuario)).GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_soporte_se_borra_al_eliminar_la_cuenta_del_jugador_y_con_el_club()
    {
        var (c, pago) = await ClubConSoporteAsync();
        var ana = await ComoAsync(c.Ana.Usuario);

        await ana.PostAsync("/api/cuenta/eliminacion", new { contrasena = Sembrador.Contrasena });

        var quedan = await _fabrica.ConContextoAsync(contexto =>
            contexto.SoportesPago.IgnoreQueryFilters().CountAsync(soporte => soporte.PagoId == pago.Id));
        Assert.Equal(0, quedan);
        var presidente = await ComoAsync(c.Presidente.Usuario);
        Assert.Equal(HttpStatusCode.NotFound, (await presidente.GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte"))).StatusCode);
    }
}
