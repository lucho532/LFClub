using System.Net;
using System.Text;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Aislamiento;

/// <summary>
/// Solo el DESARROLLADOR accede al panel de administración (constitución §20, CE-004): se prueba
/// cada endpoint que la propia API expone bajo <c>/api/plataforma/</c>, también los futuros.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AccesoPlataformaPruebas
{
    private readonly FabricaApi _fabrica;

    public AccesoPlataformaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// La notificación de la pasarela (spec 017) cuelga de <c>/api/plataforma/</c> pero no es del panel: es
    /// anónima y se protege con la firma, y la cubren <c>NotificacionPasarelaPruebas</c> y la lista de
    /// anónimos de <c>AccesoClubPruebas</c>.
    /// </summary>
    private const string NotificacionDeLaPasarela = "/api/plataforma/pagos/notificaciones";

    private List<Endpoint> DelPanel => EndpointsDeLaApi.DeLaApi(_fabrica)
        .Where(endpoint => endpoint.Ruta.StartsWith("/api/plataforma/", StringComparison.Ordinal)
            && endpoint.Ruta != NotificacionDeLaPasarela)
        .ToList();

    [Fact]
    public void El_panel_tiene_todos_los_endpoints_del_contrato()
    {
        // Si esta lista quedara vacía, las dos pruebas siguientes pasarían sin probar nada.
        Assert.Equal(18, DelPanel.Count);
    }

    [Fact]
    public async Task Sin_sesion_cada_endpoint_del_panel_responde_401()
    {
        var cliente = _fabrica.CrearClienteDePrueba();

        foreach (var endpoint in DelPanel)
        {
            var respuesta = await EnviarAsync(cliente, endpoint, Guid.NewGuid());

            Assert.True(respuesta.StatusCode == HttpStatusCode.Unauthorized, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public async Task Un_integrante_de_cualquier_rol_recibe_403_en_cada_endpoint_del_panel(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        foreach (var endpoint in DelPanel)
        {
            // Sobre su propio club, que es donde más cerca estaría de tener permiso.
            var respuesta = await EnviarAsync(cliente, endpoint, club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("solo_desarrollador", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }
    }

    internal static Task<HttpResponseMessage> EnviarAsync(ClienteDePrueba cliente, Endpoint endpoint, Guid clubId)
    {
        var peticion = new HttpRequestMessage(new HttpMethod(endpoint.Metodo), endpoint.Con(clubId));
        if (endpoint.Metodo is "POST" or "PUT")
        {
            peticion.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        return cliente.Http.SendAsync(peticion);
    }
}
