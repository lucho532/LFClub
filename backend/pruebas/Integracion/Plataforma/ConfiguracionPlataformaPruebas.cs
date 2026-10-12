using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>El DESARROLLADOR edita los datos de cualquier club (RF-010).</summary>
[Collection(ColeccionApi.Nombre)]
public class ConfiguracionPlataformaPruebas
{
    private readonly FabricaApi _fabrica;

    public ConfiguracionPlataformaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_cambio_del_desarrollador_lo_ve_el_presidente_y_el_del_presidente_lo_ve_el_desarrollador()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var delPresidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var desdeElPanel = await desarrollador.PutAsync(Ruta(club.Id), new { nombre = club.Nombre, sede = "Sede del panel" });
        var vistoPorElPresidente = await ClienteDePrueba.LeerAsync<JsonElement>(await delPresidente.GetAsync($"/api/clubes/{club.Id}"));

        await delPresidente.PutAsync($"/api/clubes/{club.Id}/configuracion", new { nombre = club.Nombre, direccion = "Dirección del presidente" });
        var vistoPorElDesarrollador = await ClienteDePrueba.LeerAsync<JsonElement>(await desarrollador.GetAsync($"/api/plataforma/clubes/{club.Id}"));

        Assert.Equal(HttpStatusCode.OK, desdeElPanel.StatusCode);
        Assert.Equal("Sede del panel", (await ClienteDePrueba.LeerAsync<JsonElement>(desdeElPanel)).GetProperty("sede").GetString());
        Assert.Equal("Sede del panel", vistoPorElPresidente.GetProperty("sede").GetString());
        Assert.Equal("Dirección del presidente", vistoPorElDesarrollador.GetProperty("direccion").GetString());
    }

    [Fact]
    public async Task Un_presidente_recibe_403_en_la_ruta_del_panel_aunque_sea_su_club()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { nombre = "Por la puerta del panel" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("solo_desarrollador", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Nombre_repetido_409_datos_no_validos_400_y_club_inexistente_404()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var repetido = await cliente.PutAsync(Ruta(club.Id), new { nombre = otroClub.Nombre });
        var invalido = await cliente.PutAsync(Ruta(club.Id), new { nombre = " " });
        var inexistente = await cliente.PutAsync(Ruta(Guid.NewGuid()), new { nombre = "Nadie" });

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal("nombre_de_club_repetido", await ClienteDePrueba.CodigoAsync(repetido));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    private static string Ruta(Guid clubId) => $"/api/plataforma/clubes/{clubId}/configuracion";
}
