using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Aislamiento;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Una cuenta en espera no accede a ninguna información del club (constitución §20, RF-016,
/// CE-005): se prueba cada endpoint que la propia API expone bajo <c>/api/clubes/{clubId}</c>,
/// también los futuros.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SalaDeEsperaPruebas
{
    private readonly FabricaApi _fabrica;

    public SalaDeEsperaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private List<Endpoint> DeClub => EndpointsDeLaApi.DeLaApi(_fabrica)
        .Where(endpoint => endpoint.Ruta.StartsWith("/api/clubes/{clubId}", StringComparison.Ordinal))
        .ToList();

    [Fact]
    public async Task Una_cuenta_en_espera_recibe_403_en_todos_los_endpoints_del_club_y_ningun_dato()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (enEspera, _) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(enEspera);
        Assert.NotEmpty(DeClub);

        foreach (var endpoint in DeClub)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(cliente, endpoint, club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("ingreso_en_espera", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }
    }

    [Theory]
    [InlineData(EstadoClub.SUSPENDIDO, "club_suspendido")]
    [InlineData(EstadoClub.DADO_DE_BAJA, "club_dado_de_baja")]
    public async Task En_un_club_que_no_esta_activo_recibe_el_motivo_del_club(EstadoClub estado, string codigo)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: estado);
        var (enEspera, _) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(enEspera);

        foreach (var endpoint in DeClub)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(cliente, endpoint, club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task Un_integrante_aprobado_del_mismo_club_sigue_entrando()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var (aprobado, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(aprobado);

        var respuesta = await cliente.GetAsync($"/api/clubes/{club.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Entra_con_el_correo_y_con_el_documento_y_su_sesion_dice_que_esta_en_espera()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (enEspera, integrante) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);

        foreach (var identificador in new[] { enEspera.Correo, integrante.NumeroDocumento })
        {
            var cliente = _fabrica.CrearClienteDePrueba();

            Assert.Equal(HttpStatusCode.OK, (await cliente.IniciarSesionAsync(identificador)).StatusCode);
            await ComprobarQueSigueEnEsperaAsync(cliente, club.Id, club.Nombre);
        }
    }

    [Fact]
    public async Task Recupera_su_contrasena_por_correo_y_sigue_en_espera()
    {
        const string nueva = "otra-contrasena-nueva";
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (enEspera, _) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var cliente = _fabrica.CrearClienteDePrueba();

        await cliente.PostAsync("/api/cuenta/recuperacion", new { correo = enEspera.Correo });
        var token = _fabrica.Correo.UltimoToken("recuperacion", enEspera.Correo);
        var confirmar = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = nueva });
        var entrar = await cliente.IniciarSesionAsync(enEspera.Correo, nueva);

        Assert.Equal(HttpStatusCode.NoContent, confirmar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, entrar.StatusCode);
        await ComprobarQueSigueEnEsperaAsync(cliente, club.Id, club.Nombre);
    }

    private static async Task ComprobarQueSigueEnEsperaAsync(ClienteDePrueba cliente, Guid clubId, string nombreClub)
    {
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        var unico = Assert.Single(sesion.GetProperty("clubes").EnumerateArray());
        Assert.Equal(clubId, unico.GetProperty("clubId").GetGuid());
        Assert.Equal(nombreClub, unico.GetProperty("nombre").GetString());
        Assert.Equal("EN_ESPERA", unico.GetProperty("estadoIngreso").GetString());
        Assert.Equal("JUGADOR", unico.GetProperty("rol").GetString());

        var club = await cliente.GetAsync($"/api/clubes/{clubId}");
        Assert.Equal(HttpStatusCode.Forbidden, club.StatusCode);
        Assert.Equal("ingreso_en_espera", await ClienteDePrueba.CodigoAsync(club));
    }
}
