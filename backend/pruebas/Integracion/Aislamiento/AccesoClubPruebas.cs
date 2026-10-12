using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Aislamiento;

/// <summary>
/// Aislamiento entre clubes y alcance del DESARROLLADOR sobre cada endpoint de club (constitución
/// §20, CE-003, RF-004), y ningún endpoint privado accesible sin sesión por omisión (§9).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AccesoClubPruebas
{
    private readonly FabricaApi _fabrica;

    public AccesoClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private List<Endpoint> DeClub => EndpointsDeLaApi.DeLaApi(_fabrica)
        .Where(endpoint => endpoint.Ruta.StartsWith("/api/clubes/{clubId}", StringComparison.Ordinal))
        .ToList();

    [Fact]
    public async Task Cada_endpoint_de_club_responde_404_a_un_integrante_de_otro_club()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        // Un presidente: el rol con más permisos, pero de otro club.
        var (ajeno, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(ajeno);
        Assert.Equal(85, DeClub.Count);

        foreach (var endpoint in DeClub)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(cliente, endpoint, club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.NotFound, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Cada_endpoint_de_club_responde_404_al_desarrollador()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        foreach (var endpoint in DeClub)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(cliente, endpoint, club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.NotFound, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public void La_api_expone_exactamente_los_endpoints_del_contrato()
    {
        var delContrato = EndpointsDeLaApi.DelContrato().Select(par => par.Endpoint.ToString()).Order().ToList();
        var deLaApi = EndpointsDeLaApi.DeLaApi(_fabrica).Select(endpoint => endpoint.ToString()).Order().ToList();

        Assert.Equal(109, delContrato.Count);
        Assert.Equal(delContrato, deLaApi);
    }

    [Fact]
    public async Task Todo_endpoint_que_el_contrato_no_marca_como_anonimo_responde_401_sin_sesion()
    {
        var contrato = EndpointsDeLaApi.DelContrato().ToDictionary(par => par.Endpoint.ToString(), par => par.Anonimo);
        var cliente = _fabrica.CrearClienteDePrueba();
        var anonimos = new List<string>();

        foreach (var endpoint in EndpointsDeLaApi.DeLaApi(_fabrica))
        {
            if (contrato[endpoint.ToString()])
            {
                anonimos.Add(endpoint.ToString());
                continue;
            }

            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(cliente, endpoint, Guid.NewGuid());

            Assert.True(respuesta.StatusCode == HttpStatusCode.Unauthorized, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        // Los anónimos son un conjunto cerrado y explícito (§9); añadir uno obliga a tocar esta lista.
        Assert.Equal(
            [
                "GET /api/publico/clubes/{clubId}/escudo",
                "POST /api/cuenta/recuperacion",
                "POST /api/cuenta/recuperacion/confirmacion",
                "POST /api/invitaciones/consulta",
                "POST /api/invitaciones/registro",
                "POST /api/sesion",
            ],
            anonimos.Order(StringComparer.Ordinal).ToList());
    }
}
