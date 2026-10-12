using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>
/// Desde el panel, solo el DESARROLLADOR cambia los colores de un club (constitución §20, RF-008). El
/// PRESIDENTE cambia los de su propio club por la ruta del club (spec 007, que deja sin efecto el
/// escenario 2 de la historia 4 de la 001): ver <c>IdentidadPorElPresidentePruebas</c>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ColoresPruebas
{
    private readonly FabricaApi _fabrica;

    public ColoresPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Guarda_y_devuelve_los_colores_aunque_tengan_poco_contraste()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        // Un color casi blanco se guarda: el aviso de contraste lo da el panel y no impide guardar (RF-009).
        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { colorPrincipal = "#b8370f", colorAcento = "#FEFEFE" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var identidad = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("identidad");
        Assert.Equal("#B8370F", identidad.GetProperty("colorPrincipal").GetString());
        Assert.Equal("#FEFEFE", identidad.GetProperty("colorAcento").GetString());

        var lista = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/plataforma/clubes"));
        var enLista = lista.EnumerateArray().Single(c => c.GetProperty("clubId").GetGuid() == club.Id);
        Assert.Equal("#B8370F", enLista.GetProperty("identidad").GetProperty("colorPrincipal").GetString());
    }

    [Theory]
    [InlineData("rojo", "#FFFFFF", "colorPrincipal")]
    [InlineData("#FFF", "#FFFFFF", "colorPrincipal")]
    [InlineData("#B8370F", "B8370F", "colorAcento")]
    [InlineData("#B8370F", "#GGGGGG", "colorAcento")]
    [InlineData("#B8370F", null, "colorAcento")]
    public async Task Un_formato_no_valido_responde_400_y_no_cambia_nada(string principal, string? acento, string campo)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { colorPrincipal = principal, colorAcento = acento });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("datos_invalidos", problema.GetProperty("codigo").GetString());
        Assert.True(problema.GetProperty("errores").TryGetProperty(campo, out _));

        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync($"/api/plataforma/clubes/{club.Id}"));
        Assert.Equal(JsonValueKind.Null, detalle.GetProperty("identidad").GetProperty("colorPrincipal").ValueKind);
    }

    [Fact]
    public async Task El_presidente_no_usa_la_ruta_del_panel_para_los_colores()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { colorPrincipal = "#B8370F", colorAcento = "#FFC72C" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("solo_desarrollador", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Un_club_inexistente_responde_404_y_sin_sesion_401()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var cuerpo = new { colorPrincipal = "#B8370F", colorAcento = "#FFC72C" };

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PutAsync(Ruta(Guid.NewGuid()), cuerpo)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fabrica.CrearClienteDePrueba().PutAsync(Ruta(Guid.NewGuid()), cuerpo)).StatusCode);
    }

    private static string Ruta(Guid clubId) => $"/api/plataforma/clubes/{clubId}/colores";
}
