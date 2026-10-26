using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// La API sin la configuración de la pasarela sigue funcionando (spec 017, RF-027): el pago en línea se
/// indica como no disponible y todo lo demás responde igual.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PasarelaNoDisponiblePruebas : IDisposable
{
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private readonly FabricaApi _fabrica;

    public PasarelaNoDisponiblePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public void Dispose()
    {
        _fabrica.Reloj.UsarElReal();
        _fabrica.CobrosPlataforma.QuitarTarifasAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task El_resumen_trae_el_cobro_y_dice_que_el_pago_en_linea_no_esta_disponible()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5));
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        using var _ = _fabrica.SinPasarela();

        var resumen = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync($"/api/clubes/{club.Id}/pago-plataforma"));
        var intento = await presidente.PostAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}/intentos");

        Assert.False(resumen.GetProperty("pagoEnLineaDisponible").GetBoolean());
        Assert.Equal(cobro.Id, resumen.GetProperty("pendientes")[0].GetProperty("cobroId").GetGuid());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, intento.StatusCode);
        Assert.Equal("pago_no_disponible", await ClienteDePrueba.CodigoAsync(intento));
    }

    [Fact]
    public async Task Las_tarifas_dicen_que_la_pasarela_no_esta_configurada_sin_devolver_valores()
    {
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        using var _ = _fabrica.SinPasarela();

        var respuesta = await desarrollador.GetAsync("/api/plataforma/tarifas");

        var tarifas = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.False(tarifas.GetProperty("pasarelaConfigurada").GetBoolean());
        Assert.DoesNotContain("wompi", (await respuesta.Content.ReadAsStringAsync()), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task El_trabajo_sigue_generando_cobros_y_suspendiendo_y_la_marca_manual_reactiva()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(precio: 1000, dias: 0);
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        await _fabrica.CobrosPlataforma.FijarInicioAsync(club, Noviembre);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        using var _ = _fabrica.SinPasarela();
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 3), new TimeOnly(10, 0));

        await _fabrica.CobrosPlataforma.GenerarAsync();
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();

        var cobro = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        Assert.Equal(EstadoClub.SUSPENDIDO, (await _fabrica.CobrosPlataforma.ClubAsync(club)).Estado);

        var marcar = await desarrollador.PostAsync($"/api/plataforma/clubes/{club.Id}/cobros/{cobro.Id}/pago-manual");

        Assert.Equal(HttpStatusCode.OK, marcar.StatusCode);
        Assert.Equal(EstadoClub.ACTIVO, (await _fabrica.CobrosPlataforma.ClubAsync(club)).Estado);
    }

    [Fact]
    public async Task El_resto_de_la_aplicacion_responde_con_normalidad()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        using var _ = _fabrica.SinPasarela();

        Assert.Equal(HttpStatusCode.OK, (await presidente.GetAsync($"/api/clubes/{club.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await desarrollador.GetAsync("/api/plataforma/clubes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await presidente.GetAsync($"/api/clubes/{club.Id}/pago-plataforma")).StatusCode);
        var cobros = await _fabrica.ConContextoAsync(c => c.CobrosPlataforma.CountAsync(x => x.ClubId == club.Id));
        Assert.Equal(0, cobros);
    }
}
