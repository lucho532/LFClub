using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Clubes;

/// <summary>Cada club se ve con su propia identidad (constitución §7.2, RF-023).</summary>
[Collection(ColeccionApi.Nombre)]
public class IdentidadPruebas
{
    private readonly FabricaApi _fabrica;

    public IdentidadPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_sesion_y_el_club_devuelven_la_identidad_de_cada_club()
    {
        var naranja = await _fabrica.Sembrador.CrearClubAsync();
        var azul = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var documento = Sembrador.Unico("doc");
        await _fabrica.Sembrador.CrearIntegranteAsync(naranja, usuario, Rol.PRESIDENTE, documento);
        await _fabrica.Sembrador.CrearIntegranteAsync(azul, usuario, Rol.DIRECTIVO, documento);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await desarrollador.PutAsync($"/api/plataforma/clubes/{naranja.Id}/colores", new { colorPrincipal = "#B8370F", colorAcento = "#FFC72C" });
        await desarrollador.PutArchivoAsync($"/api/plataforma/clubes/{naranja.Id}/escudo", Imagenes.Png());
        await desarrollador.PutAsync($"/api/plataforma/clubes/{azul.Id}/colores", new { colorPrincipal = "#1D4E89", colorAcento = "#E1ECF8" });
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        var clubNaranja = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync($"/api/clubes/{naranja.Id}"));
        var clubAzul = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync($"/api/clubes/{azul.Id}"));

        var deSesion = sesion.GetProperty("clubes").EnumerateArray()
            .ToDictionary(c => c.GetProperty("clubId").GetGuid(), c => c.GetProperty("identidad"));
        Assert.Equal("#B8370F", deSesion[naranja.Id].GetProperty("colorPrincipal").GetString());
        Assert.Equal($"/api/publico/clubes/{naranja.Id}/escudo?v=1", deSesion[naranja.Id].GetProperty("urlEscudo").GetString());
        Assert.Equal("#1D4E89", deSesion[azul.Id].GetProperty("colorPrincipal").GetString());
        Assert.Equal(JsonValueKind.Null, deSesion[azul.Id].GetProperty("urlEscudo").ValueKind);

        Assert.Equal("#FFC72C", clubNaranja.GetProperty("identidad").GetProperty("colorAcento").GetString());
        Assert.Equal("#E1ECF8", clubAzul.GetProperty("identidad").GetProperty("colorAcento").GetString());
    }

    [Fact]
    public async Task Un_club_recien_creado_tiene_todos_los_campos_de_identidad_nulos()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var delClub = (await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync($"/api/clubes/{club.Id}"))).GetProperty("identidad");
        var deSesion = (await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"))).GetProperty("clubes")[0].GetProperty("identidad");
        var deInvitacion = (await ClienteDePrueba.LeerAsync<JsonElement>(
            await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/consulta", new { token }))).GetProperty("identidad");

        foreach (var identidad in new[] { delClub, deSesion, deInvitacion })
        {
            Assert.Equal(JsonValueKind.Null, identidad.GetProperty("colorPrincipal").ValueKind);
            Assert.Equal(JsonValueKind.Null, identidad.GetProperty("colorAcento").ValueKind);
            Assert.Equal(JsonValueKind.Null, identidad.GetProperty("urlEscudo").ValueKind);
        }
    }
}
