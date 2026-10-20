using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>Solo el DESARROLLADOR accede al panel de administración (constitución §20, RF-003).</summary>
[Collection(ColeccionApi.Nombre)]
public class ListaClubesPruebas
{
    private readonly FabricaApi _fabrica;

    public ListaClubesPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_desarrollador_ve_todos_los_clubes_con_su_estado_y_si_tienen_presidente()
    {
        var conPresidente = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(conPresidente, Rol.PRESIDENTE);
        var soloDirectivo = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        await _fabrica.Sembrador.CrearIntegranteAsync(soloDirectivo, Rol.DIRECTIVO);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.GetAsync("/api/plataforma/clubes");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var clubes = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).EnumerateArray().ToList();
        var primero = clubes.Single(club => club.GetProperty("clubId").GetGuid() == conPresidente.Id);
        var segundo = clubes.Single(club => club.GetProperty("clubId").GetGuid() == soloDirectivo.Id);
        Assert.Equal(conPresidente.Nombre, primero.GetProperty("nombre").GetString());
        Assert.Equal("ACTIVO", primero.GetProperty("estado").GetString());
        Assert.True(primero.GetProperty("presidenteRegistrado").GetBoolean());
        Assert.Equal("SUSPENDIDO", segundo.GetProperty("estado").GetString());
        Assert.False(segundo.GetProperty("presidenteRegistrado").GetBoolean());
        Assert.Equal(JsonValueKind.Null, segundo.GetProperty("identidad").GetProperty("colorPrincipal").ValueKind);
    }

    [Fact]
    public async Task Cada_club_trae_sus_integrantes_activos_contados_por_rol()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otro = await _fabrica.Sembrador.CrearClubAsync();
        foreach (var rol in new[] { Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR, Rol.JUGADOR, Rol.JUGADOR })
            await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        await _fabrica.Sembrador.CrearIntegranteAsync(otro, Rol.JUGADOR);
        var (_, retirado) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        await _fabrica.ConContextoAsync<bool>(async contexto =>
        {
            var integrante = await contexto.UsuariosRol.IgnoreQueryFilters().SingleAsync(i => i.Id == retirado.Id);
            integrante.Activo = false;
            await contexto.SaveChangesAsync();
            return true;
        });
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var lista = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/plataforma/clubes"));

        var conteo = lista.EnumerateArray().Single(c => c.GetProperty("clubId").GetGuid() == club.Id).GetProperty("integrantes");
        Assert.Equal(2, conteo.GetProperty("jugadores").GetInt32());
        Assert.Equal(1, conteo.GetProperty("entrenadores").GetInt32());
        Assert.Equal(1, conteo.GetProperty("directivos").GetInt32());
        Assert.Equal(1, conteo.GetProperty("presidentes").GetInt32());
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var respuesta = await _fabrica.CrearClienteDePrueba().GetAsync("/api/plataforma/clubes");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public async Task Cualquier_otro_rol_recibe_403_sin_ningun_dato(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var lista = await cliente.GetAsync("/api/plataforma/clubes");
        var detalle = await cliente.GetAsync($"/api/plataforma/clubes/{club.Id}");
        var crear = await cliente.PostAsync("/api/plataforma/clubes", new { nombre = "No debería", correoPresidente = "a@b.co" });

        foreach (var respuesta in new[] { lista, detalle, crear })
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("solo_desarrollador", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }
    }
}
