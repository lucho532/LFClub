using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Reenviar y cancelar invitaciones del club, que solo hace su PRESIDENTE y solo con las
/// pendientes. Es la continuación de <see cref="InvitacionesClubPruebas"/>, separada para no pasar
/// de 250 líneas.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ReenvioYCancelacionPruebas
{
    private readonly FabricaApi _fabrica;

    public ReenvioYCancelacionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Reenviar_entrega_un_enlace_nuevo_invalida_el_anterior_y_deja_una_sola_fila()
    {
        var (club, cliente) = await ClubConAsync(Rol.PRESIDENTE);
        var correo = Sembrador.CorreoUnico();
        var primera = await InvitarAsync(cliente, club, correo);
        var tokenAnterior = _fabrica.Correo.UltimoToken("invitacion", correo);

        var respuesta = await cliente.PostAsync($"{Ruta(club)}/{primera}/reenvio");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var nueva = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.NotEqual(primera, nueva.GetProperty("invitacionId").GetGuid());
        Assert.Equal(correo, nueva.GetProperty("correo").GetString());
        Assert.Equal("PENDIENTE", nueva.GetProperty("estado").GetString());

        var tokenNuevo = _fabrica.Correo.UltimoToken("invitacion", correo);
        Assert.NotEqual(tokenAnterior, tokenNuevo);
        Assert.Equal(HttpStatusCode.Gone, await ConsultarAsync(tokenAnterior));
        Assert.Equal(HttpStatusCode.OK, await ConsultarAsync(tokenNuevo));
        Assert.DoesNotContain(tokenNuevo, await respuesta.Content.ReadAsStringAsync());

        var fila = Assert.Single(await ListarAsync(cliente, club));
        Assert.Equal(nueva.GetProperty("invitacionId").GetGuid(), fila.GetProperty("invitacionId").GetGuid());
    }

    [Fact]
    public async Task Cancelar_deja_la_fila_como_cancelada_y_su_enlace_ya_no_sirve()
    {
        var (club, cliente) = await ClubConAsync(Rol.PRESIDENTE);
        var correo = Sembrador.CorreoUnico();
        var invitacionId = await InvitarAsync(cliente, club, correo);
        var token = _fabrica.Correo.UltimoToken("invitacion", correo);

        var respuesta = await cliente.PostAsync($"{Ruta(club)}/{invitacionId}/cancelacion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("CANCELADA", (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.Gone, await ConsultarAsync(token));

        var fila = Assert.Single(await ListarAsync(cliente, club));
        Assert.Equal(invitacionId, fila.GetProperty("invitacionId").GetGuid());
        Assert.Equal("CANCELADA", fila.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Reenviar_o_cancelar_una_usada_una_vencida_o_una_cancelada_responde_409()
    {
        var (club, cliente) = await ClubConAsync(Rol.PRESIDENTE);
        var (usada, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, usadaEn: DateTime.UtcNow, rol: Rol.JUGADOR);
        var (vencida, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, venceEn: DateTime.UtcNow.AddMinutes(-1), rol: Rol.JUGADOR);
        var (cancelada, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, anuladaEn: DateTime.UtcNow, rol: Rol.JUGADOR);

        foreach (var invitacion in new[] { usada, vencida, cancelada })
        {
            foreach (var accion in new[] { "reenvio", "cancelacion" })
            {
                var respuesta = await cliente.PostAsync($"{Ruta(club)}/{invitacion.Id}/{accion}");

                Assert.True(respuesta.StatusCode == HttpStatusCode.Conflict, $"{accion}: {(int)respuesta.StatusCode}");
                Assert.Equal("invitacion_no_pendiente", await ClienteDePrueba.CodigoAsync(respuesta));
            }

            Assert.Equal(0, _fabrica.Correo.Contar("invitacion", invitacion.Correo));
        }

        Assert.Equal(3, (await ListarAsync(cliente, club)).Count);
    }

    [Fact]
    public async Task Una_invitacion_inexistente_o_de_presidente_responde_404()
    {
        var (club, cliente) = await ClubConAsync(Rol.PRESIDENTE);
        var (dePresidente, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);

        foreach (var invitacionId in new[] { Guid.NewGuid(), dePresidente.Id })
        {
            foreach (var accion in new[] { "reenvio", "cancelacion" })
            {
                var respuesta = await cliente.PostAsync($"{Ruta(club)}/{invitacionId}/{accion}");

                Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
                Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            }
        }

        // La invitación de presidente no aparece en la lista del club y sigue sirviendo.
        Assert.Empty(await ListarAsync(cliente, club));
        Assert.Equal(HttpStatusCode.OK, await ConsultarAsync(token));
    }

    [Fact]
    public async Task La_invitacion_sigue_sirviendo_aunque_quien_la_envio_deje_de_ser_presidente_o_salga_del_club()
    {
        // Un club con dos presidentes: el segundo invita y después deja de serlo.
        var (club, delPresidente) = await ClubConAsync(Rol.PRESIDENTE);
        var (quienInvita, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var delQueInvita = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(quienInvita);
        var correo = Sembrador.CorreoUnico();
        await InvitarAsync(delQueInvita, club, correo);
        var token = _fabrica.Correo.UltimoToken("invitacion", correo);

        // Deja de ser presidente: la invitación sirve y sigue diciendo quién la envió.
        await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().Where(i => i.Id == integrante.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(i => i.Rol, Rol.JUGADOR)));
        Assert.Equal(HttpStatusCode.OK, await ConsultarAsync(token));
        var conNombre = Assert.Single(await ListarAsync(delPresidente, club));
        Assert.Equal($"{integrante.Nombres} {integrante.Apellidos}", conNombre.GetProperty("enviadaPor").GetString());

        // Sale del club: sigue sirviendo y `enviadaPor` llega nulo.
        await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().Where(i => i.Id == integrante.Id).ExecuteDeleteAsync());
        Assert.Equal(HttpStatusCode.OK, await ConsultarAsync(token));
        var sinNombre = Assert.Single(await ListarAsync(delPresidente, club));
        Assert.Equal(JsonValueKind.Null, sinNombre.GetProperty("enviadaPor").ValueKind);
    }

    private static string Ruta(Club club) => $"/api/clubes/{club.Id}/invitaciones";

    private async Task<(Club Club, ClienteDePrueba Cliente)> ClubConAsync(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        return (club, await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario));
    }

    private static async Task<Guid> InvitarAsync(ClienteDePrueba cliente, Club club, string correo)
    {
        var respuesta = await cliente.PostAsync(Ruta(club), new { correo, rol = "JUGADOR" });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("invitacionId").GetGuid();
    }

    private static async Task<List<JsonElement>> ListarAsync(ClienteDePrueba cliente, Club club) =>
        (await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync(Ruta(club)))).EnumerateArray().ToList();

    private async Task<HttpStatusCode> ConsultarAsync(string token) =>
        (await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/consulta", new { token })).StatusCode;
}
