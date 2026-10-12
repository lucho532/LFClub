using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Aislamiento;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// Un jugador retirado no accede a ninguna información del club y conserva todos sus datos
/// (constitución §14.1 y §20; RF-043; historia 6, escenarios 3 a 5; CE-014). Se recorren los
/// endpoints que la propia API expone, así que los futuros quedan cubiertos.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AccesoDelRetiradoPruebas
{
    private readonly FabricaApi _fabrica;

    public AccesoDelRetiradoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private List<Endpoint> DeClub => EndpointsDeLaApi.DeLaApi(_fabrica)
        .Where(endpoint => endpoint.Ruta.StartsWith("/api/clubes/{clubId}", StringComparison.Ordinal))
        .ToList();

    [Fact]
    public async Task Un_jugador_retirado_recibe_403_en_cada_endpoint_del_club_sin_ningun_dato()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.Sembrar.RetirarAsync(e.IntegranteJugador, e.IntegrantePresidente);
        Assert.True(DeClub.Count >= 32);

        foreach (var endpoint in DeClub)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(e.Jugador, endpoint, e.Club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(e.Club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task En_un_club_suspendido_recibe_el_motivo_del_club()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        var (_, presidente) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (usuario, jugador) = await _fabrica.Categorias.CrearJugadorAsync(club, 2014);
        await _fabrica.Categorias.RetirarAsync(jugador, presidente);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.GetAsync($"/api/clubes/{club.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("club_suspendido", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task La_sesion_dice_en_que_club_esta_retirado_y_en_los_demas_sigue_entrando()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var usuario = await _fabrica.ConContextoAsync(contexto =>
            Task.FromResult(contexto.Usuarios.Single(cuenta => cuenta.Id == e.IntegranteJugador.UsuarioId)));
        await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, usuario, Rol.JUGADOR);
        await e.Sembrar.RetirarAsync(e.IntegranteJugador, e.IntegrantePresidente);

        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await e.Jugador.GetAsync("/api/sesion"));
        var clubes = sesion.GetProperty("clubes").EnumerateArray()
            .ToDictionary(club => club.GetProperty("clubId").GetGuid(), club => club.GetProperty("retirado").GetBoolean());

        // El retiro es de cada club: trae los dos, con su nombre y su identidad, y solo uno retirado.
        Assert.True(clubes[e.Club.Id]);
        Assert.False(clubes[e.OtroClub.Id]);
        Assert.Equal(HttpStatusCode.OK, (await e.Jugador.GetAsync($"/api/clubes/{e.OtroClub.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await e.Jugador.GetAsync($"/api/clubes/{e.Club.Id}")).StatusCode);
    }

    [Fact]
    public async Task Retirarlo_por_la_api_se_aplica_a_su_sesion_ya_abierta_y_no_borra_ningun_dato_suyo()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var antes = (await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!;
        Assert.Equal(HttpStatusCode.OK, (await e.Jugador.GetAsync($"/api/clubes/{e.Club.Id}")).StatusCode);

        await e.Presidente.PostAsync(e.Retiro(e.IntegranteJugador.Id));

        // El mismo token que entraba hace un momento.
        var respuesta = await e.Jugador.GetAsync($"/api/clubes/{e.Club.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(respuesta));

        var despues = (await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!;
        Assert.Equal(antes.Nombres, despues.Nombres);
        Assert.Equal(antes.Apellidos, despues.Apellidos);
        Assert.Equal(antes.NumeroDocumento, despues.NumeroDocumento);
        Assert.Equal(antes.FechaNacimiento, despues.FechaNacimiento);
        Assert.Equal(antes.UsuarioId, despues.UsuarioId);
        Assert.Equal(Rol.JUGADOR, despues.Rol);
        Assert.Equal(EstadoIngreso.APROBADO, despues.EstadoIngreso);
        Assert.True(await _fabrica.ConContextoAsync(contexto =>
            Task.FromResult(contexto.Usuarios.Any(cuenta => cuenta.Id == antes.UsuarioId))));
    }
}
