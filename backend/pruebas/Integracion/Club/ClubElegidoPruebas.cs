using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Clubes;

/// <summary>
/// Aislamiento entre clubes (constitución §20, RF-024 y RF-004): conocer el identificador de un
/// club no concede acceso.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ClubElegidoPruebas
{
    private readonly FabricaApi _fabrica;

    public ClubElegidoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.JUGADOR)]
    public async Task Un_integrante_obtiene_su_club_con_su_rol(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.GetAsync($"/api/clubes/{club.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var dto = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(club.Id, dto.GetProperty("clubId").GetGuid());
        Assert.Equal(club.Nombre, dto.GetProperty("nombre").GetString());
        Assert.Equal(rol.ToString(), dto.GetProperty("miRol").GetString());
        Assert.Equal(integrante.Id, dto.GetProperty("miUsuarioRolId").GetGuid());
        Assert.Equal("ACTIVO", dto.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Dos_integrantes_del_mismo_club_reciben_cada_uno_su_identificador_de_integrante()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, integrantePresidente) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (jugador, integranteJugador) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);

        var delPresidente = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente)).GetAsync($"/api/clubes/{club.Id}"));
        var delJugador = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(jugador)).GetAsync($"/api/clubes/{club.Id}"));

        Assert.Equal(integrantePresidente.Id, delPresidente.GetProperty("miUsuarioRolId").GetGuid());
        Assert.Equal(integranteJugador.Id, delJugador.GetProperty("miUsuarioRolId").GetGuid());
    }

    [Fact]
    public async Task Un_integrante_de_otro_club_recibe_404_aunque_conozca_el_identificador()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (ajeno, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(ajeno);

        var respuesta = await cliente.GetAsync($"/api/clubes/{club.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.DoesNotContain(club.Nombre, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_desarrollador_recibe_el_mismo_404_que_para_un_club_inexistente()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var existente = await cliente.GetAsync($"/api/clubes/{club.Id}");
        var inexistente = await cliente.GetAsync($"/api/clubes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, existente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await existente.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();

        var respuesta = await _fabrica.CrearClienteDePrueba().GetAsync($"/api/clubes/{club.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(respuesta));
    }
}
