using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Clubes;

/// <summary>
/// En un club suspendido solo entra su PRESIDENTE; en uno dado de baja no entra nadie (constitución
/// §20, RF-027, RF-028 y RF-030). Se aplica también a las sesiones ya abiertas.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AccesoPorEstadoPruebas
{
    private readonly FabricaApi _fabrica;

    public AccesoPorEstadoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task En_un_club_suspendido_entra_el_presidente_y_ve_que_esta_suspendido()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var respuesta = await cliente.GetAsync($"/api/clubes/{club.Id}");
        var edicion = await cliente.PutAsync($"/api/clubes/{club.Id}/configuracion", new { nombre = club.Nombre, sede = "Sede" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("SUSPENDIDO", (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public async Task En_un_club_suspendido_los_demas_reciben_403_club_suspendido(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.GetAsync($"/api/clubes/{club.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("club_suspendido", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Contains("presidente", await ClienteDePrueba.TituloAsync(respuesta));
        Assert.DoesNotContain(club.Nombre, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Quien_pertenece_a_un_club_suspendido_y_a_otro_activo_usa_el_activo_con_normalidad()
    {
        var suspendido = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        var activo = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var documento = Sembrador.Unico("doc");
        await _fabrica.Sembrador.CrearIntegranteAsync(suspendido, usuario, Rol.DIRECTIVO, documento);
        await _fabrica.Sembrador.CrearIntegranteAsync(activo, usuario, Rol.DIRECTIVO, documento);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var enSuspendido = await cliente.GetAsync($"/api/clubes/{suspendido.Id}");
        var enActivo = await cliente.GetAsync($"/api/clubes/{activo.Id}");
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));

        Assert.Equal(HttpStatusCode.Forbidden, enSuspendido.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enActivo.StatusCode);
        // La sesión sigue listando los dos, con su estado, para que el desplegable permita cambiar.
        var estados = sesion.GetProperty("clubes").EnumerateArray()
            .ToDictionary(c => c.GetProperty("clubId").GetGuid(), c => c.GetProperty("estado").GetString());
        Assert.Equal("SUSPENDIDO", estados[suspendido.Id]);
        Assert.Equal("ACTIVO", estados[activo.Id]);
    }

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.JUGADOR)]
    public async Task En_un_club_dado_de_baja_no_entra_nadie_tampoco_el_presidente(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.DADO_DE_BAJA);
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.GetAsync($"/api/clubes/{club.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("club_dado_de_baja", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Una_sesion_abierta_antes_del_cambio_recibe_la_respuesta_nueva_y_vuelve_a_entrar_al_revertirlo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (directivo, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        // Los dos tokens se emiten con el club activo y no se renuevan en toda la prueba.
        var delPresidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
        var delDirectivo = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(directivo);
        var ruta = $"/api/clubes/{club.Id}";
        var estado = $"/api/plataforma/clubes/{club.Id}/estado";
        Assert.Equal(HttpStatusCode.OK, (await delDirectivo.GetAsync(ruta)).StatusCode);

        await desarrollador.PutAsync(estado, new { estado = "SUSPENDIDO" });
        Assert.Equal(HttpStatusCode.OK, (await delPresidente.GetAsync(ruta)).StatusCode);
        Assert.Equal("club_suspendido", await ClienteDePrueba.CodigoAsync(await delDirectivo.GetAsync(ruta)));

        await desarrollador.PutAsync(estado, new { estado = "ACTIVO" });
        Assert.Equal(HttpStatusCode.OK, (await delDirectivo.GetAsync(ruta)).StatusCode);

        await desarrollador.PutAsync(estado, new { estado = "DADO_DE_BAJA" });
        Assert.Equal("club_dado_de_baja", await ClienteDePrueba.CodigoAsync(await delPresidente.GetAsync(ruta)));
        Assert.Equal("club_dado_de_baja", await ClienteDePrueba.CodigoAsync(await delDirectivo.GetAsync(ruta)));

        await desarrollador.PutAsync(estado, new { estado = "ACTIVO" });
        Assert.Equal(HttpStatusCode.OK, (await delPresidente.GetAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await delDirectivo.GetAsync(ruta)).StatusCode);
    }
}
