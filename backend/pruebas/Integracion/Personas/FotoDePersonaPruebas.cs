using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using static LaPecosa.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LaPecosa.Pruebas.Integracion.Personas;

/// <summary>
/// La foto de perfil de un PRESIDENTE, DIRECTIVO o ENTRENADOR la ve todo su club y nadie de fuera;
/// la de una cuenta que en el club es JUGADOR sigue privada (spec 008, RF-007 a RF-009; CE-003;
/// constitución §14.2).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FotoDePersonaPruebas
{
    private readonly FabricaApi _fabrica;

    public FotoDePersonaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_foto_de_E1_la_ven_la_familia_E2_D_y_P_sin_cache()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var quienes = new[]
        {
            await ClienteAsync(c.Ana.Usuario, c.Ana.Integrante),
            await ClienteAsync(c.E2.Usuario),
            await ClienteAsync(c.D.Usuario),
            await ClienteAsync(c.P.Usuario),
        };

        foreach (var cliente in quienes)
        {
            var respuesta = await cliente.GetAsync(c.Foto(c.E1.Integrante));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("image/jpeg", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Imagenes.Jpeg(), await respuesta.Content.ReadAsByteArrayAsync());
            Assert.True(respuesta.Headers.CacheControl?.Private);
            Assert.True(respuesta.Headers.CacheControl?.NoStore);
            Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
        }
    }

    [Fact]
    public async Task Sin_foto_responde_404()
    {
        var c = await _fabrica.Personas.CrearAsync();

        await NoEncontradoAsync(await (await ClienteAsync(c.P.Usuario)).GetAsync(c.Foto(c.D.Integrante)));
    }

    [Fact]
    public async Task El_presidente_de_otro_club_y_el_desarrollador_reciben_404()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        await NoEncontradoAsync(await (await ClienteAsync(c.PresidenteB.Usuario)).GetAsync(c.Foto(c.E1.Integrante)));
        await NoEncontradoAsync(await desarrollador.GetAsync(c.Foto(c.E1.Integrante)));
    }

    [Fact]
    public async Task La_foto_de_perfil_de_la_cuenta_de_un_jugador_no_sale_por_el_club()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Personas.PonerFotoPerfilAsync(c.Ana.Usuario, Imagenes.Png());

        await NoEncontradoAsync(await (await ClienteAsync(c.P.Usuario)).GetAsync(c.Foto(c.Ana.Integrante)));
    }

    [Fact]
    public async Task Una_cuenta_entrenador_en_A_y_jugador_en_B_solo_muestra_su_foto_en_A()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var (cuenta, enA) = await _fabrica.Personas.CrearPersonaAsync(c.Club, Rol.ENTRENADOR, "Mixto", "Dosclubes");
        var enB = await _fabrica.Sembrador.CrearIntegranteAsync(c.ClubB, cuenta, Rol.JUGADOR);
        await _fabrica.Personas.PonerFotoPerfilAsync(cuenta, Imagenes.Webp(), "image/webp");

        var porA = await (await ClienteAsync(c.P.Usuario)).GetAsync(c.Foto(enA));
        var porB = await (await ClienteAsync(c.PresidenteB.Usuario)).GetAsync($"/api/clubes/{c.ClubB.Id}/personas/{enB.Id}/foto");

        Assert.Equal(HttpStatusCode.OK, porA.StatusCode);
        Assert.Equal(Imagenes.Webp(), await porA.Content.ReadAsByteArrayAsync());
        await NoEncontradoAsync(porB);
    }

    [Fact]
    public async Task E1_desde_el_club_B_da_la_misma_foto()
    {
        var c = await _fabrica.Personas.CrearAsync();

        var respuesta = await (await ClienteAsync(c.PresidenteB.Usuario)).GetAsync($"/api/clubes/{c.ClubB.Id}/personas/{c.E1EnB.Id}/foto");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(Imagenes.Jpeg(), await respuesta.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Al_cambiar_la_foto_desde_mi_perfil_sube_la_version_de_la_ficha_basica()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var e1 = await ClienteAsync(c.E1.Usuario);
        var antes = (await LeerAsync(e1, c.Ficha(c.E1.Integrante))).GetProperty("versionFoto").GetInt32();

        var cambio = await e1.PutArchivoAsync("/api/cuenta/foto", Imagenes.Png(), "nueva.png");

        Assert.True(cambio.IsSuccessStatusCode, $"{(int)cambio.StatusCode}");
        var despues = (await LeerAsync(e1, c.Ficha(c.E1.Integrante))).GetProperty("versionFoto").GetInt32();
        Assert.True(despues > antes);
        Assert.Equal(Imagenes.Png(), await (await e1.GetAsync(c.Foto(c.E1.Integrante))).Content.ReadAsByteArrayAsync());
    }

    private Task<ClienteDePrueba> ClienteAsync(Usuario cuenta, UsuarioRol? elegido = null) =>
        _fabrica.CrearClienteDePrueba().ElegirJugador(elegido?.Id).ConSesionDeAsync(cuenta);
}
