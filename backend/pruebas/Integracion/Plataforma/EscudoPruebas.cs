using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>
/// Escudo del club: desde el panel lo carga solo el DESARROLLADOR, y se obtiene sin sesión (RF-008,
/// research §10). El PRESIDENTE carga el de su propio club por la ruta del club (spec 007): ver
/// <c>IdentidadPorElPresidentePruebas</c>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EscudoPruebas
{
    private readonly FabricaApi _fabrica;

    public EscudoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public static TheoryData<byte[], string> Formatos => new()
    {
        { Imagenes.Png(), "image/png" },
        { Imagenes.Jpeg(), "image/jpeg" },
        { Imagenes.Webp(), "image/webp" },
    };

    [Theory]
    [MemberData(nameof(Formatos))]
    public async Task Se_guarda_y_se_obtiene_sin_sesion_con_el_tipo_detectado_por_su_firma(byte[] imagen, string tipo)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        // El nombre y el tipo declarados mienten: manda la firma binaria.
        var respuesta = await cliente.PutArchivoAsync(Ruta(club.Id), imagen, "escudo.svg");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var url = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("identidad").GetProperty("urlEscudo").GetString();
        Assert.Equal($"/api/publico/clubes/{club.Id}/escudo?v=1", url);

        var publico = await _fabrica.CrearClienteDePrueba().GetAsync(url!);
        Assert.Equal(HttpStatusCode.OK, publico.StatusCode);
        Assert.Equal(tipo, publico.Content.Headers.ContentType?.MediaType);
        Assert.Equal(imagen, await publico.Content.ReadAsByteArrayAsync());
        Assert.Contains("immutable", publico.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Lo_que_no_es_imagen_o_pasa_de_1_mb_se_rechaza_y_se_conserva_el_escudo_anterior()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var anterior = Imagenes.Png(32);
        await cliente.PutArchivoAsync(Ruta(club.Id), anterior);

        var texto = await cliente.PutArchivoAsync(Ruta(club.Id), Imagenes.Texto(), "escudo.png");
        var svg = await cliente.PutArchivoAsync(Ruta(club.Id), Imagenes.Svg(), "escudo.svg");
        var grande = await cliente.PutArchivoAsync(Ruta(club.Id), Imagenes.PngDeMasDe1Mb());
        var sinArchivo = await cliente.Http.PutAsync(Ruta(club.Id), new MultipartFormDataContent { { new StringContent("x"), "otro" } });

        Assert.Equal(HttpStatusCode.BadRequest, texto.StatusCode);
        Assert.Equal("escudo_no_es_imagen", await ClienteDePrueba.CodigoAsync(texto));
        Assert.Equal("escudo_no_es_imagen", await ClienteDePrueba.CodigoAsync(svg));
        Assert.Equal(HttpStatusCode.BadRequest, grande.StatusCode);
        Assert.Equal("escudo_demasiado_grande", await ClienteDePrueba.CodigoAsync(grande));
        Assert.Equal("escudo_no_es_imagen", await ClienteDePrueba.CodigoAsync(sinArchivo));

        var publico = await _fabrica.CrearClienteDePrueba().GetAsync($"/api/publico/clubes/{club.Id}/escudo?v=1");
        Assert.Equal(anterior, await publico.Content.ReadAsByteArrayAsync());
        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync($"/api/plataforma/clubes/{club.Id}"));
        Assert.EndsWith("?v=1", detalle.GetProperty("identidad").GetProperty("urlEscudo").GetString());
    }

    [Fact]
    public async Task Al_reemplazarlo_cambia_la_version_de_su_direccion()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await cliente.PutArchivoAsync(Ruta(club.Id), Imagenes.Png());

        var respuesta = await cliente.PutArchivoAsync(Ruta(club.Id), Imagenes.Jpeg());

        var url = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("identidad").GetProperty("urlEscudo").GetString();
        Assert.EndsWith("?v=2", url);
        var publico = await _fabrica.CrearClienteDePrueba().GetAsync(url!);
        Assert.Equal("image/jpeg", publico.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Un_club_sin_escudo_o_inexistente_responde_404_y_el_de_otro_club_no_se_mezcla()
    {
        var conEscudo = await _fabrica.Sembrador.CrearClubAsync();
        var sinEscudo = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await cliente.PutArchivoAsync(Ruta(conEscudo.Id), Imagenes.Png());
        var anonimo = _fabrica.CrearClienteDePrueba();

        var sin = await anonimo.GetAsync($"/api/publico/clubes/{sinEscudo.Id}/escudo");
        var inexistente = await anonimo.GetAsync($"/api/publico/clubes/{Guid.NewGuid()}/escudo");

        Assert.Equal(HttpStatusCode.NotFound, sin.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(sin));
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Theory]
    [InlineData(EstadoClub.SUSPENDIDO)]
    [InlineData(EstadoClub.DADO_DE_BAJA)]
    public async Task Se_sirve_en_cualquier_estado_del_club(EstadoClub estado)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: estado);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await cliente.PutArchivoAsync(Ruta(club.Id), Imagenes.Webp());

        var publico = await _fabrica.CrearClienteDePrueba().GetAsync($"/api/publico/clubes/{club.Id}/escudo");

        Assert.Equal(HttpStatusCode.OK, publico.StatusCode);
    }

    [Fact]
    public async Task Desde_el_panel_solo_lo_carga_el_desarrollador()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var delPresidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var conSesion = await delPresidente.PutArchivoAsync(Ruta(club.Id), Imagenes.Png());
        var sinSesion = await _fabrica.CrearClienteDePrueba().PutArchivoAsync(Ruta(club.Id), Imagenes.Png());

        Assert.Equal(HttpStatusCode.Forbidden, conSesion.StatusCode);
        Assert.Equal("solo_desarrollador", await ClienteDePrueba.CodigoAsync(conSesion));
        Assert.Equal(HttpStatusCode.Unauthorized, sinSesion.StatusCode);
    }

    private static string Ruta(Guid clubId) => $"/api/plataforma/clubes/{clubId}/escudo";
}
