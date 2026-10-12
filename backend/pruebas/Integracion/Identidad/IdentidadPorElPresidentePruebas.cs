using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Identidad;

/// <summary>
/// El PRESIDENTE cambia el escudo y los colores de su club con las mismas reglas que el panel, y
/// nadie más del club ni de otro club puede hacerlo (spec 007, historia 4; RF-017 a RF-022; CE-005 y
/// CE-006; constitución §7.2 y §20).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class IdentidadPorElPresidentePruebas
{
    private static readonly object Naranja = new { colorPrincipal = "#b8370f", colorAcento = "#FFC72C" };

    private readonly FabricaApi _fabrica;

    public IdentidadPorElPresidentePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Colores(Club club) => $"/api/clubes/{club.Id}/colores";

    private static string Escudo(Club club) => $"/api/clubes/{club.Id}/escudo";

    [Fact]
    public async Task El_presidente_cambia_colores_y_escudo_y_el_panel_y_su_club_lo_muestran()
    {
        var (club, presidente) = await ClubConPresidenteAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();

        var colores = await LeerClubAsync(await presidente.PutAsync(Colores(club), Naranja));
        var conEscudo = await LeerClubAsync(await presidente.PutArchivoAsync(Escudo(club), Imagenes.Png()));
        var otroEscudo = await LeerClubAsync(await presidente.PutArchivoAsync(Escudo(club), Imagenes.Jpeg()));

        Assert.Equal("#B8370F", colores.GetProperty("identidad").GetProperty("colorPrincipal").GetString());
        Assert.Equal("PRESIDENTE", colores.GetProperty("miRol").GetString());
        Assert.Equal($"/api/publico/clubes/{club.Id}/escudo?v=1", UrlEscudo(conEscudo));
        Assert.Equal($"/api/publico/clubes/{club.Id}/escudo?v=2", UrlEscudo(otroEscudo));

        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var delPanel = await ClienteDePrueba.LeerAsync<JsonElement>(await desarrollador.GetAsync($"/api/plataforma/clubes/{club.Id}"));
        Assert.Equal("#FFC72C", delPanel.GetProperty("identidad").GetProperty("colorAcento").GetString());
        Assert.Equal(UrlEscudo(otroEscudo), UrlEscudo(delPanel));
        var delClub = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync($"/api/clubes/{club.Id}"));
        Assert.Equal("#B8370F", delClub.GetProperty("identidad").GetProperty("colorPrincipal").GetString());
        var publico = await _fabrica.CrearClienteDePrueba().GetAsync($"/api/publico/clubes/{club.Id}/escudo");
        Assert.Equal(Imagenes.Jpeg(), await publico.Content.ReadAsByteArrayAsync());

        // El club B no cambió en ningún momento.
        var deB = await ClienteDePrueba.LeerAsync<JsonElement>(await desarrollador.GetAsync($"/api/plataforma/clubes/{otroClub.Id}"));
        Assert.Equal(JsonValueKind.Null, deB.GetProperty("identidad").GetProperty("colorPrincipal").ValueKind);
        Assert.Equal(JsonValueKind.Null, deB.GetProperty("identidad").GetProperty("urlEscudo").ValueKind);
    }

    [Fact]
    public async Task Un_escudo_rechazado_conserva_el_anterior_y_unos_colores_mal_formados_dan_400()
    {
        var (club, presidente) = await ClubConPresidenteAsync();
        await LeerClubAsync(await presidente.PutArchivoAsync(Escudo(club), Imagenes.Png()));

        var grande = await presidente.PutArchivoAsync(Escudo(club), Imagenes.PngDeMasDe1Mb());
        var texto = await presidente.PutArchivoAsync(Escudo(club), Imagenes.Texto(), "escudo.png");
        var malos = await presidente.PutAsync(Colores(club), new { colorPrincipal = "rojo", colorAcento = "#FFC72C" });

        Assert.Equal(HttpStatusCode.BadRequest, grande.StatusCode);
        Assert.Equal("escudo_demasiado_grande", await ClienteDePrueba.CodigoAsync(grande));
        Assert.Equal("escudo_no_es_imagen", await ClienteDePrueba.CodigoAsync(texto));
        Assert.Equal(HttpStatusCode.BadRequest, malos.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(malos));
        var delClub = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync($"/api/clubes/{club.Id}"));
        Assert.Equal($"/api/publico/clubes/{club.Id}/escudo?v=1", UrlEscudo(delClub));
        Assert.Equal(JsonValueKind.Null, delClub.GetProperty("identidad").GetProperty("colorPrincipal").ValueKind);
    }

    [Fact]
    public async Task Los_demas_roles_del_club_reciben_403_rol_no_autorizado()
    {
        var (club, _) = await ClubConPresidenteAsync();

        foreach (var rol in new[] { Rol.DIRECTIVO, Rol.ENTRENADOR, Rol.JUGADOR })
        {
            var (cuenta, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
            var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

            var colores = await cliente.PutAsync(Colores(club), Naranja);
            var escudo = await cliente.PutArchivoAsync(Escudo(club), Imagenes.Png());

            Assert.Equal(HttpStatusCode.Forbidden, colores.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(colores));
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(escudo));
        }
    }

    [Fact]
    public async Task El_presidente_de_otro_club_recibe_404_y_el_club_no_cambia()
    {
        var (club, _) = await ClubConPresidenteAsync();
        var (_, ajeno) = await ClubConPresidenteAsync();

        var colores = await ajeno.PutAsync(Colores(club), Naranja);
        var escudo = await ajeno.PutArchivoAsync(Escudo(club), Imagenes.Png());

        Assert.Equal(HttpStatusCode.NotFound, colores.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(colores));
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(escudo));
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var delPanel = await ClienteDePrueba.LeerAsync<JsonElement>(await desarrollador.GetAsync($"/api/plataforma/clubes/{club.Id}"));
        Assert.Equal(JsonValueKind.Null, delPanel.GetProperty("identidad").GetProperty("colorPrincipal").ValueKind);
        Assert.Equal(JsonValueKind.Null, delPanel.GetProperty("identidad").GetProperty("urlEscudo").ValueKind);
    }

    [Fact]
    public async Task Con_el_club_suspendido_el_presidente_sigue_pudiendo_cambiarla()
    {
        var (club, presidente) = await ClubConPresidenteAsync(EstadoClub.SUSPENDIDO);

        var colores = await LeerClubAsync(await presidente.PutAsync(Colores(club), Naranja));
        var escudo = await LeerClubAsync(await presidente.PutArchivoAsync(Escudo(club), Imagenes.Webp()));

        Assert.Equal("#B8370F", colores.GetProperty("identidad").GetProperty("colorPrincipal").GetString());
        Assert.Equal($"/api/publico/clubes/{club.Id}/escudo?v=1", UrlEscudo(escudo));
    }

    [Fact]
    public async Task Vale_el_ultimo_cambio_venga_del_presidente_o_del_desarrollador()
    {
        var (club, presidente) = await ClubConPresidenteAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        await LeerClubAsync(await presidente.PutAsync(Colores(club), Naranja));
        Assert.Equal(HttpStatusCode.OK, (await desarrollador.PutAsync(
            $"/api/plataforma/clubes/{club.Id}/colores", new { colorPrincipal = "#1D4E89", colorAcento = "#E1ECF8" })).StatusCode);

        var delClub = await ClienteDePrueba.LeerAsync<JsonElement>(await presidente.GetAsync($"/api/clubes/{club.Id}"));
        Assert.Equal("#1D4E89", delClub.GetProperty("identidad").GetProperty("colorPrincipal").GetString());

        var otraVez = await LeerClubAsync(await presidente.PutAsync(Colores(club), Naranja));
        Assert.Equal("#B8370F", otraVez.GetProperty("identidad").GetProperty("colorPrincipal").GetString());
    }

    private async Task<(Club Club, ClienteDePrueba Presidente)> ClubConPresidenteAsync(EstadoClub estado = EstadoClub.ACTIVO)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: estado);
        var (cuenta, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        return (club, await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta));
    }

    private static string? UrlEscudo(JsonElement club) => club.GetProperty("identidad").GetProperty("urlEscudo").GetString();

    private static async Task<JsonElement> LeerClubAsync(HttpResponseMessage respuesta)
    {
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, $"{(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
        return await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
    }
}
