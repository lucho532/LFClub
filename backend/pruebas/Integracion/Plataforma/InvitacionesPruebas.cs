using System.Net;
using System.Text.Json;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>
/// La invitación del presidente desde el panel: el DESARROLLADOR la reenvía o corrige su correo
/// mientras no se use, y ya no puede invitar a un presidente a un club que existe (constitución
/// §12.5 y §20; RF-024 y RF-025; historia 4; CE-010).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class InvitacionesPruebas
{
    private readonly FabricaApi _fabrica;

    public InvitacionesPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Reenviar_invalida_el_enlace_anterior_y_envia_uno_nuevo_al_mismo_correo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (anterior, tokenAnterior) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync($"/api/plataforma/clubes/{club.Id}/invitaciones/{anterior.Id}/reenvio");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var nueva = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.NotEqual(anterior.Id, nueva.GetProperty("invitacionId").GetGuid());
        Assert.Equal(anterior.Correo, nueva.GetProperty("correo").GetString());
        Assert.Equal("ENVIADO", nueva.GetProperty("estadoEnvio").GetString());

        var tokenNuevo = _fabrica.Correo.UltimoToken("invitacion", anterior.Correo);
        Assert.NotEqual(tokenAnterior, tokenNuevo);
        Assert.False(await EstaVigenteAsync(tokenAnterior));
        Assert.True(await EstaVigenteAsync(tokenNuevo));

        var detalle = await DetalleAsync(cliente, club.Id);
        var pendiente = Assert.Single(detalle.GetProperty("invitaciones").EnumerateArray());
        Assert.Equal(nueva.GetProperty("invitacionId").GetGuid(), pendiente.GetProperty("invitacionId").GetGuid());
    }

    [Fact]
    public async Task Corregir_el_correo_envia_la_invitacion_al_nuevo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (anterior, tokenAnterior) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var corregido = Sembrador.CorreoUnico();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(
            $"/api/plataforma/clubes/{club.Id}/invitaciones/{anterior.Id}/reenvio", new { correo = $" {corregido.ToUpperInvariant()}" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(corregido, (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("correo").GetString());
        Assert.Equal(1, _fabrica.Correo.Contar("invitacion", corregido));
        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", anterior.Correo));
        Assert.False(await EstaVigenteAsync(tokenAnterior));
    }

    [Fact]
    public async Task Una_invitacion_vencida_aparece_como_vencida_y_se_puede_reenviar()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (vencida, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, venceEn: DateTime.UtcNow.AddMinutes(-1));
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var antes = await DetalleAsync(cliente, club.Id);
        var respuesta = await cliente.PostAsync($"/api/plataforma/clubes/{club.Id}/invitaciones/{vencida.Id}/reenvio");

        Assert.True(antes.GetProperty("invitaciones")[0].GetProperty("vencida").GetBoolean());
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.False((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("vencida").GetBoolean());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task El_desarrollador_no_puede_invitar_a_un_presidente_a_un_club_que_ya_existe(bool yaTienePresidente)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        if (yaTienePresidente)
        {
            await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        }

        var correo = Sembrador.CorreoUnico();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync($"/api/plataforma/clubes/{club.Id}/invitaciones", new { correo });

        // La operación no existe: ni se niega ni se valida, y no se crea ni se envía nada.
        Assert.Contains(respuesta.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", correo));
        Assert.Empty((await DetalleAsync(cliente, club.Id)).GetProperty("invitaciones").EnumerateArray());
        Assert.Equal(0, await _fabrica.ConContextoAsync(contexto =>
            contexto.Invitaciones.IgnoreQueryFilters().CountAsync(invitacion => invitacion.ClubId == club.Id)));
    }

    [Fact]
    public async Task El_correo_del_desarrollador_responde_409_al_corregir()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (pendiente, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var correo = $" {FabricaApi.CorreoDesarrollador.ToUpperInvariant()} ";

        var corregir = await cliente.PostAsync($"/api/plataforma/clubes/{club.Id}/invitaciones/{pendiente.Id}/reenvio", new { correo });

        Assert.Equal(HttpStatusCode.Conflict, corregir.StatusCode);
        Assert.Equal("correo_del_desarrollador", await ClienteDePrueba.CodigoAsync(corregir));
        Assert.True(await EstaVigenteAsync(token));
    }

    [Fact]
    public async Task Reenviar_una_invitacion_ya_usada_responde_409()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usada, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, usadaEn: DateTime.UtcNow);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync($"/api/plataforma/clubes/{club.Id}/invitaciones/{usada.Id}/reenvio");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("invitacion_ya_usada", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Un_club_inexistente_o_una_invitacion_de_otro_club_responden_404()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otro = await _fabrica.Sembrador.CrearClubAsync();
        var (deOtro, _) = await _fabrica.Sembrador.CrearInvitacionAsync(otro);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var detalle = await cliente.GetAsync($"/api/plataforma/clubes/{Guid.NewGuid()}");
        var enOtroClub = await cliente.PostAsync($"/api/plataforma/clubes/{Guid.NewGuid()}/invitaciones/{deOtro.Id}/reenvio");
        var reenviar = await cliente.PostAsync($"/api/plataforma/clubes/{club.Id}/invitaciones/{deOtro.Id}/reenvio");

        foreach (var respuesta in new[] { detalle, enOtroClub, reenviar })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task Un_correo_mal_escrito_al_corregir_responde_400_y_la_invitacion_sigue_sirviendo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (pendiente, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(
            $"/api/plataforma/clubes/{club.Id}/invitaciones/{pendiente.Id}/reenvio", new { correo = "sin-arroba" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.True(await EstaVigenteAsync(token));
    }

    [Fact]
    public async Task Solo_el_desarrollador_reenvia()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (pendiente, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var conSesion = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
        var sinSesion = _fabrica.CrearClienteDePrueba();
        var reenviar = $"/api/plataforma/clubes/{club.Id}/invitaciones/{pendiente.Id}/reenvio";

        Assert.Equal(HttpStatusCode.Forbidden, (await conSesion.PostAsync(reenviar)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await sinSesion.PostAsync(reenviar)).StatusCode);
        Assert.True(await EstaVigenteAsync(token));
    }

    [Fact]
    public async Task Las_invitaciones_que_envia_el_club_no_se_ven_ni_se_tocan_desde_el_panel()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var correo = Sembrador.CorreoUnico();
        var (delClub, tokenDelClub) = await _fabrica.Sembrador.CrearInvitacionAsync(club, correo, rol: Rol.JUGADOR);
        var (dePresidente, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        // El detalle del panel solo muestra las de presidente (RF-029).
        var detalle = await cliente.GetAsync($"/api/plataforma/clubes/{club.Id}");
        var unica = Assert.Single((await ClienteDePrueba.LeerAsync<JsonElement>(detalle)).GetProperty("invitaciones").EnumerateArray());
        Assert.Equal(dePresidente.Id, unica.GetProperty("invitacionId").GetGuid());
        Assert.DoesNotContain(correo, await detalle.Content.ReadAsStringAsync());

        // El reenvío del panel no alcanza la invitación del club.
        var reenviar = await cliente.PostAsync($"/api/plataforma/clubes/{club.Id}/invitaciones/{delClub.Id}/reenvio");
        Assert.Equal(HttpStatusCode.NotFound, reenviar.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(reenviar));
        Assert.True(await EstaVigenteAsync(tokenDelClub));
    }

    private static async Task<JsonElement> DetalleAsync(ClienteDePrueba cliente, Guid clubId) =>
        await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync($"/api/plataforma/clubes/{clubId}"));

    private async Task<bool> EstaVigenteAsync(string token)
    {
        var hash = GeneradorTokens.Hash(token);
        Invitacion invitacion = await _fabrica.ConContextoAsync(contexto =>
            contexto.Invitaciones.IgnoreQueryFilters().AsNoTracking().SingleAsync(i => i.TokenHash == hash));
        return invitacion.EstaVigente(DateTime.UtcNow);
    }
}
