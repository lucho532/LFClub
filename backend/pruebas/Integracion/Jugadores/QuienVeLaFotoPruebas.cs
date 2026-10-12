using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Jugadores;

/// <summary>
/// La foto de un jugador la ve quien ve su ficha, y nadie más: a los demás se les responde como si
/// no existiera (spec 007, historia 2; RF-008 y RF-010; constitución §14.2).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class QuienVeLaFotoPruebas
{
    private readonly FabricaApi _fabrica;

    public QuienVeLaFotoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_ven_su_cuenta_el_presidente_el_directivo_y_el_entrenador_de_su_categoria()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var bytes = Imagenes.Jpeg(64);
        await _fabrica.Jugadores.PonerFotoAsync(c.Ana.Integrante, bytes, "image/jpeg");
        var quienes = new[]
        {
            await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, c.Ana.Integrante),
            await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario),
            await _fabrica.Jugadores.ClienteAsync(c.Directivo.Usuario),
            await _fabrica.Jugadores.ClienteAsync(c.E1.Usuario),
        };

        foreach (var cliente in quienes)
        {
            var respuesta = await cliente.GetAsync(c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/foto"));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("image/jpeg", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(bytes, await respuesta.Content.ReadAsByteArrayAsync());
            Assert.True(respuesta.Headers.CacheControl?.Private);
            Assert.True(respuesta.Headers.CacheControl?.NoStore);
        }
    }

    [Fact]
    public async Task Quien_no_ve_la_ficha_recibe_404_como_si_no_existiera()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        await _fabrica.Jugadores.PonerFotoAsync(c.Ana.Integrante, Imagenes.Png());
        var (cuentaE2015, entrenador2015) = await _fabrica.Sembrador.CrearIntegranteAsync(c.Club, Rol.ENTRENADOR);
        await _fabrica.Categorias.AsignarEntrenadorAsync(c.C2015, entrenador2015);
        var hermano = await _fabrica.Hermanos.CrearHermanoAsync(
            c.Ana.Integrante, EstadoIngreso.APROBADO, nombres: "Teo", categoria: c.C2014);
        var quienes = new[]
        {
            await _fabrica.Jugadores.ClienteAsync(c.E2.Usuario),
            await _fabrica.Jugadores.ClienteAsync(cuentaE2015),
            await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, hermano),
            await _fabrica.Jugadores.ClienteAsync(c.Caro.Usuario),
        };

        foreach (var cliente in quienes)
        {
            var respuesta = await cliente.GetAsync(c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/foto"));

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task Un_jugador_sin_foto_o_un_identificador_inexistente_dan_el_mismo_404()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var presidente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);

        foreach (var id in new[] { c.Beto.Integrante.Id, Guid.NewGuid(), c.Luis.Id })
        {
            var respuesta = await presidente.GetAsync(c.Ruta($"/jugadores/{id}/foto"));

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task La_ficha_trae_la_fecha_de_la_foto_o_null()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var foto = await _fabrica.Jugadores.PonerFotoAsync(c.Ana.Integrante, Imagenes.Png());
        var presidente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);

        var conFoto = await FichaAsync(presidente, c, c.Ana.Integrante.Id);
        var sinFoto = await FichaAsync(presidente, c, c.Beto.Integrante.Id);

        Assert.Equal(foto.GuardadaEn, conFoto.GetProperty("fotoActualizadaEn").GetDateTime().ToUniversalTime(), TimeSpan.FromMilliseconds(1));
        Assert.Equal(JsonValueKind.Null, sinFoto.GetProperty("fotoActualizadaEn").ValueKind);
    }

    internal static async Task<JsonElement> FichaAsync(ClienteDePrueba cliente, ClubDeJugadores c, Guid usuarioRolId)
    {
        var respuesta = await cliente.GetAsync(c.Ruta($"/jugadores/{usuarioRolId}/ficha"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
    }
}
