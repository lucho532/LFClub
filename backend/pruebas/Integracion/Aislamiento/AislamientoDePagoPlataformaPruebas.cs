using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Aislamiento;

/// <summary>
/// Un club no ve ni paga el cobro de otro (constitución §7.1, spec 017). Las tablas del cobro no tienen
/// filtro global, así que esta prueba cubre el filtro explícito de sus repositorios.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AislamientoDePagoPlataformaPruebas
{
    private readonly FabricaApi _fabrica;

    public AislamientoDePagoPlataformaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_presidente_no_ve_ni_inicia_el_pago_de_un_cobro_de_otro_club_por_su_identificador()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var ajeno = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cobroAjeno = await _fabrica.CobrosPlataforma.CrearCobroAsync(ajeno, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        // Por la ruta de su propio club, con el identificador del cobro ajeno.
        var ver = await presidente.GetAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobroAjeno.Id}");
        var pagar = await presidente.PostAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobroAjeno.Id}/intentos");
        var resumen = await presidente.GetAsync($"/api/clubes/{club.Id}/pago-plataforma");

        Assert.Equal(HttpStatusCode.NotFound, ver.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, pagar.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(pagar));
        Assert.DoesNotContain(cobroAjeno.Id.ToString(), await resumen.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Con_varios_presidentes_lo_ven_los_dos()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        foreach (var _ in new[] { 1, 2 })
        {
            var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
            var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

            var respuesta = await presidente.GetAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }
    }

    [Fact]
    public async Task El_desarrollador_no_tiene_ruta_de_club_para_el_cobro()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await desarrollador.GetAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
