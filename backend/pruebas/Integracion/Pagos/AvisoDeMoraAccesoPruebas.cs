using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// Quién ve el aviso fijo de mora: solo la cuenta del propio jugador; el resto del club, otras familias,
/// otros clubes y los retirados no (spec 016, historia 3; RF-014 a RF-017; CE-006; constitución §16.9).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisoDeMoraAccesoPruebas
{
    private static readonly DateOnly Octubre = new(2026, 10, 1);

    private readonly FabricaApi _fabrica;

    public AvisoDeMoraAccesoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private static string Ruta(ClubDePagos c, UsuarioRol jugador) => c.Ruta($"/jugadores/{jugador.Id}/cuenta/aviso");

    [Fact]
    public async Task Solo_el_propio_jugador_lo_ve_y_las_respuestas_no_se_guardan_en_cache()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var respuesta = await (await ComoAsync(c.Ana.Usuario)).GetAsync(Ruta(c, c.Ana.Integrante));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no-store", respuesta.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Otra_familia_responde_404_sin_revelar_que_el_jugador_existe()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var otra = await (await ComoAsync(c.Beto.Usuario)).GetAsync(Ruta(c, c.Ana.Integrante));

        Assert.Equal(HttpStatusCode.NotFound, otra.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(otra));
    }

    [Fact]
    public async Task Un_jugador_de_otro_club_responde_404()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var otroClub = await _fabrica.Pagos.CrearClubAsync();

        var respuesta = await (await ComoAsync(otroClub.Ana.Usuario)).GetAsync(Ruta(c, c.Ana.Integrante));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_jugador_retirado_responde_404()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);
        var cliente = await ComoAsync(c.Ana.Usuario);
        await _fabrica.Categorias.RetirarAsync(c.Ana.Integrante, c.Presidente.Integrante);

        var respuesta = await cliente.GetAsync(Ruta(c, c.Ana.Integrante));

        Assert.True(respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden, $"{(int)respuesta.StatusCode}");
    }

    [Fact]
    public async Task El_presidente_el_directivo_y_el_entrenador_reciben_403_aunque_entrene_al_jugador()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);

        foreach (var quien in new[] { c.Presidente, c.Directivo, c.Entrenador })
        {
            var respuesta = await (await ComoAsync(quien.Usuario)).GetAsync(Ruta(c, c.Ana.Integrante));

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var respuesta = await _fabrica.CrearClienteDePrueba().GetAsync(Ruta(c, c.Ana.Integrante));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Estar_en_mora_no_restringe_la_cuenta_ni_otras_pantallas()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);
        var cliente = await ComoAsync(c.Ana.Usuario);

        var aviso = await ClienteDePrueba.LeerAsync<System.Text.Json.JsonElement>(await cliente.GetAsync(Ruta(c, c.Ana.Integrante)));
        Assert.Equal("EN_MORA", aviso.GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(c.Cuenta(c.Ana.Integrante))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/ficha"))).StatusCode);
    }
}
