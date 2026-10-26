using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>El detalle de un club en el panel trae cuántos integrantes activos tiene por rol.</summary>
[Collection(ColeccionApi.Nombre)]
public class DetalleClubIntegrantesPruebas
{
    private readonly FabricaApi _fabrica;

    public DetalleClubIntegrantesPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_detalle_del_club_trae_el_conteo_por_rol_y_ningun_dato_personal()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, presidente) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR);
        var otro = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(otro, Rol.JUGADOR);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await desarrollador.GetAsync($"/api/plataforma/clubes/{club.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var detalle = JsonSerializer.Deserialize<JsonElement>(await respuesta.Content.ReadAsStringAsync());
        var integrantes = detalle.GetProperty("integrantes");
        Assert.Equal(2, integrantes.GetProperty("jugadores").GetInt32());
        Assert.Equal(1, integrantes.GetProperty("entrenadores").GetInt32());
        Assert.Equal(0, integrantes.GetProperty("directivos").GetInt32());
        Assert.Equal(1, integrantes.GetProperty("presidentes").GetInt32());
        string[] esperados = ["directivos", "entrenadores", "jugadores", "presidentes"];
        Assert.Equal(esperados, integrantes.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.NotNull(presidente);
    }
}
