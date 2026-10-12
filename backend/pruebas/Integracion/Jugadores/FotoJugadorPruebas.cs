using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Jugadores.QuienVeLaFotoPruebas;

namespace LFClub.Pruebas.Integracion.Jugadores;

/// <summary>
/// La familia y el PRESIDENTE cargan, cambian y quitan la foto propia del jugador; cada cambio
/// sella la ficha y no toca la foto de un hermano ni la de perfil de la cuenta (spec 007, historia
/// 2; RF-009; constitución §14.2).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FotoJugadorPruebas
{
    private readonly FabricaApi _fabrica;

    public FotoJugadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_cuenta_del_jugador_la_carga_y_la_cambia_y_el_ultimo_cambio_lo_dice()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var familia = await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, c.Ana.Integrante);
        var ruta = Foto(c, c.Ana.Integrante.Id);

        var primera = await LeerFichaAsync(await familia.PutArchivoAsync(ruta, Imagenes.Png(), "foto.png"));
        var segunda = await LeerFichaAsync(await familia.PutArchivoAsync(ruta, Imagenes.Jpeg(), "foto.jpg"));

        var fechaPrimera = primera.GetProperty("fotoActualizadaEn").GetDateTime();
        Assert.True(segunda.GetProperty("fotoActualizadaEn").GetDateTime() > fechaPrimera);
        Assert.True(segunda.GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());
        var servida = await familia.GetAsync(ruta);
        Assert.Equal("image/jpeg", servida.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Imagenes.Jpeg(), await servida.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task El_presidente_la_carga_y_el_ultimo_cambio_dice_que_fue_el()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var presidente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);

        var ficha = await LeerFichaAsync(await presidente.PutArchivoAsync(Foto(c, c.Beto.Integrante.Id), Imagenes.Webp()));

        var cambio = ficha.GetProperty("ultimoCambio");
        Assert.Equal($"{c.Presidente.Integrante.Nombres} {c.Presidente.Integrante.Apellidos}", cambio.GetProperty("autor").GetString());
        Assert.False(cambio.GetProperty("porLaCuentaDelJugador").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, ficha.GetProperty("fotoActualizadaEn").ValueKind);
    }

    [Fact]
    public async Task Una_imagen_rechazada_conserva_la_anterior()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var presidente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);
        var ruta = Foto(c, c.Ana.Integrante.Id);
        await LeerFichaAsync(await presidente.PutArchivoAsync(ruta, Imagenes.Png()));

        var grande = await presidente.PutArchivoAsync(ruta, Imagenes.PngDeMasDe1Mb());
        var texto = await presidente.PutArchivoAsync(ruta, Imagenes.Texto(), "falsa.png");
        var svg = await presidente.PutArchivoAsync(ruta, Imagenes.Svg(), "dibujo.svg");

        Assert.Equal(HttpStatusCode.BadRequest, grande.StatusCode);
        Assert.Equal("foto_demasiado_grande", await ClienteDePrueba.CodigoAsync(grande));
        Assert.Equal("foto_no_es_imagen", await ClienteDePrueba.CodigoAsync(texto));
        Assert.Equal("foto_no_es_imagen", await ClienteDePrueba.CodigoAsync(svg));
        Assert.Equal(Imagenes.Png(), await (await presidente.GetAsync(ruta)).Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Quitarla_deja_las_iniciales_y_sella_la_ficha()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        await _fabrica.Jugadores.PonerFotoAsync(c.Ana.Integrante, Imagenes.Png());
        var familia = await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, c.Ana.Integrante);
        var ruta = Foto(c, c.Ana.Integrante.Id);

        var ficha = await LeerFichaAsync(await familia.DeleteAsync(ruta));
        var otraVez = await LeerFichaAsync(await familia.DeleteAsync(ruta));

        Assert.Equal(JsonValueKind.Null, ficha.GetProperty("fotoActualizadaEn").ValueKind);
        Assert.True(ficha.GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());
        Assert.Equal(JsonValueKind.Null, otraVez.GetProperty("fotoActualizadaEn").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await familia.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task La_foto_de_un_jugador_no_cambia_la_de_su_hermano_ni_la_de_perfil_de_la_cuenta()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var hermano = await _fabrica.Hermanos.CrearHermanoAsync(
            c.Ana.Integrante, EstadoIngreso.APROBADO, nombres: "Teo", categoria: c.C2014);
        await _fabrica.Jugadores.PonerFotoAsync(hermano, Imagenes.Webp());
        var familia = await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, c.Ana.Integrante);
        Assert.Equal(HttpStatusCode.OK, (await familia.PutArchivoAsync("/api/cuenta/foto", Imagenes.Jpeg(32))).StatusCode);

        await LeerFichaAsync(await familia.PutArchivoAsync(Foto(c, c.Ana.Integrante.Id), Imagenes.Png()));

        var presidente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);
        Assert.Equal(Imagenes.Webp(), await (await presidente.GetAsync(Foto(c, hermano.Id))).Content.ReadAsByteArrayAsync());
        Assert.Equal(Imagenes.Jpeg(32), await (await familia.GetAsync("/api/cuenta/foto")).Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task El_directivo_y_el_entrenador_no_la_cargan_ni_la_quitan()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var ruta = Foto(c, c.Ana.Integrante.Id);

        foreach (var cuenta in new[] { c.Directivo.Usuario, c.E1.Usuario })
        {
            var cliente = await _fabrica.Jugadores.ClienteAsync(cuenta);
            var carga = await cliente.PutArchivoAsync(ruta, Imagenes.Png());
            var quita = await cliente.DeleteAsync(ruta);

            Assert.Equal(HttpStatusCode.Forbidden, carga.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(carga));
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(quita));
        }
    }

    [Fact]
    public async Task La_familia_no_llega_a_la_foto_de_otro_jugador()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var familia = await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, c.Ana.Integrante);

        var carga = await familia.PutArchivoAsync(Foto(c, c.Beto.Integrante.Id), Imagenes.Png());
        var quita = await familia.DeleteAsync(Foto(c, c.Beto.Integrante.Id));

        Assert.Equal(HttpStatusCode.NotFound, carga.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(carga));
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(quita));
    }

    [Fact]
    public async Task A_un_retirado_no_se_le_carga_foto_y_quitarla_no_falla()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var presidente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);
        var ruta = Foto(c, c.Eva.Integrante.Id);

        var carga = await presidente.PutArchivoAsync(ruta, Imagenes.Png());
        var quita = await presidente.DeleteAsync(ruta);

        Assert.Equal(HttpStatusCode.Conflict, carga.StatusCode);
        Assert.Equal("jugador_retirado", await ClienteDePrueba.CodigoAsync(carga));
        Assert.Equal(JsonValueKind.Null, (await LeerFichaAsync(quita)).GetProperty("fotoActualizadaEn").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await presidente.GetAsync(ruta)).StatusCode);
        Assert.True((await FichaAsync(presidente, c, c.Eva.Integrante.Id)).GetProperty("retirado").GetBoolean());
    }

    private static string Foto(ClubDeJugadores c, Guid usuarioRolId) => c.Ruta($"/jugadores/{usuarioRolId}/foto");

    private static async Task<JsonElement> LeerFichaAsync(HttpResponseMessage respuesta)
    {
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, $"{(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
        return await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
    }
}
