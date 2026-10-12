using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Hermanos;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Jugadores.QuienVeLaFotoPruebas;

namespace LFClub.Pruebas.Integracion.Jugadores;

/// <summary>
/// La foto propia del jugador se borra definitivamente cuando deja el club, de cualquiera de las
/// tres formas, y al reincorporarlo vuelve sin foto (spec 007, historia 2; RF-009; constitución
/// §14.2).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FotoAlRetirarPruebas
{
    private readonly FabricaApi _fabrica;

    public FotoAlRetirarPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<bool> TieneFotoAsync(Guid usuarioRolId) => _fabrica.ConContextoAsync(contexto =>
        contexto.FotosJugador.IgnoreQueryFilters().AnyAsync(foto => foto.UsuarioRolId == usuarioRolId));

    [Fact]
    public async Task Retirarlo_el_presidente_borra_su_foto_y_reincorporado_vuelve_sin_ella()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        await _fabrica.Jugadores.PonerFotoAsync(c.Beto.Integrante, Imagenes.Png());
        await _fabrica.Jugadores.PonerFotoAsync(c.Ana.Integrante, Imagenes.Jpeg());
        var presidente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);

        var retiro = await presidente.PostAsync(c.Ruta($"/jugadores/{c.Beto.Integrante.Id}/retiro"));

        Assert.Equal(HttpStatusCode.NoContent, retiro.StatusCode);
        Assert.False(await TieneFotoAsync(c.Beto.Integrante.Id));
        Assert.True(await TieneFotoAsync(c.Ana.Integrante.Id));

        var vuelta = await presidente.PostAsync(c.Ruta($"/jugadores/{c.Beto.Integrante.Id}/reincorporacion"));

        Assert.Equal(HttpStatusCode.OK, vuelta.StatusCode);
        var ficha = await FichaAsync(presidente, c, c.Beto.Integrante.Id);
        Assert.False(ficha.GetProperty("retirado").GetBoolean());
        Assert.Equal(JsonValueKind.Null, ficha.GetProperty("fotoActualizadaEn").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await presidente.GetAsync(c.Ruta($"/jugadores/{c.Beto.Integrante.Id}/foto"))).StatusCode);
    }

    [Fact]
    public async Task Si_la_familia_retira_a_su_jugador_tambien_pierde_la_foto()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        await _fabrica.Jugadores.PonerFotoAsync(c.Ana.Integrante, Imagenes.Png());
        var familia = await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, c.Ana.Integrante);

        var retiro = await familia.PostAsync(c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/retiro"));

        Assert.Equal(HttpStatusCode.NoContent, retiro.StatusCode);
        Assert.False(await TieneFotoAsync(c.Ana.Integrante.Id));
    }

    [Fact]
    public async Task Quien_queda_retirado_al_aprobarse_su_hermano_en_otro_club_pierde_la_foto()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        await _fabrica.Jugadores.PonerFotoAsync(e.Caro, Imagenes.Png());
        await _fabrica.Jugadores.PonerFotoAsync(e.Dani, Imagenes.Jpeg());
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);

        var aprobacion = await e.Presidente.PostAsync($"/api/clubes/{e.Club.Id}/ingresos/{hermano.Id}/aprobacion");

        Assert.Equal(HttpStatusCode.OK, aprobacion.StatusCode);
        Assert.False(await TieneFotoAsync(e.Caro.Id));
        Assert.True(await TieneFotoAsync(e.Dani.Id));
    }
}
