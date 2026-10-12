using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Las invitaciones, la sala de espera y las aprobaciones de un club son inaccesibles para otro
/// club, también por identificador, y para el DESARROLLADOR (constitución §20, RF-028 y RF-029).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AislamientoIngresosPruebas
{
    private readonly FabricaApi _fabrica;

    public AislamientoIngresosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_presidente_de_otro_club_recibe_404_con_el_identificador_real_de_una_invitacion_y_no_la_cambia()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.JUGADOR);
        var ajeno = await PresidenteDeOtroClubAsync();
        var suClub = await ClubDeAsync(ajeno);
        var respuestas = new[]
        {
            await ajeno.GetAsync($"/api/clubes/{club.Id}/invitaciones"),
            await ajeno.PostAsync($"/api/clubes/{club.Id}/invitaciones", new { correo = Sembrador.CorreoUnico(), rol = "JUGADOR" }),
            await ajeno.PostAsync($"/api/clubes/{club.Id}/invitaciones/{invitacion.Id}/reenvio"),
            await ajeno.PostAsync($"/api/clubes/{club.Id}/invitaciones/{invitacion.Id}/cancelacion"),
            // Tampoco desde su propio club, con el identificador de la invitación ajena.
            await ajeno.PostAsync($"/api/clubes/{suClub}/invitaciones/{invitacion.Id}/reenvio"),
            await ajeno.PostAsync($"/api/clubes/{suClub}/invitaciones/{invitacion.Id}/cancelacion"),
        };

        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(invitacion.Correo, await respuesta.Content.ReadAsStringAsync());
        }

        var actual = await InvitacionAsync(invitacion.Id);
        Assert.Null(actual.AnuladaEn);
        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", invitacion.Correo));
        Assert.Equal(
            HttpStatusCode.OK,
            (await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/consulta", new { token })).StatusCode);
        // Su propia lista no incluye la invitación del otro club.
        Assert.DoesNotContain(
            invitacion.Correo, await (await ajeno.GetAsync($"/api/clubes/{suClub}/invitaciones")).Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_desarrollador_recibe_404_en_las_invitaciones_del_club()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (invitacion, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.JUGADOR);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var respuestas = new[]
        {
            await desarrollador.GetAsync($"/api/clubes/{club.Id}/invitaciones"),
            await desarrollador.PostAsync($"/api/clubes/{club.Id}/invitaciones", new { correo = Sembrador.CorreoUnico(), rol = "JUGADOR" }),
            await desarrollador.PostAsync($"/api/clubes/{club.Id}/invitaciones/{invitacion.Id}/reenvio"),
            await desarrollador.PostAsync($"/api/clubes/{club.Id}/invitaciones/{invitacion.Id}/cancelacion"),
        };

        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Null((await InvitacionAsync(invitacion.Id)).AnuladaEn);
    }

    [Fact]
    public async Task El_presidente_de_otro_club_y_el_desarrollador_reciben_404_en_los_ingresos_y_no_cambian_nada()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var presidente = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);
        var (cuenta, enEspera) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var (_, aprobado) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        await presidente.PostAsync(EscenarioIngresos.Aprobacion(club, aprobado.Id), new { rol = "JUGADOR" });
        var ajeno = await PresidenteDeOtroClubAsync();
        var suClub = await ClubDeAsync(ajeno);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        foreach (var cliente in new[] { ajeno, desarrollador })
        {
            var respuestas = new[]
            {
                await cliente.GetAsync(EscenarioIngresos.EnEspera(club)),
                await cliente.GetAsync(EscenarioIngresos.Aprobados(club)),
                await cliente.PostAsync(EscenarioIngresos.Aprobacion(club, enEspera.Id), new { rol = "JUGADOR" }),
                await cliente.PostAsync(EscenarioIngresos.Rechazo(club, enEspera.Id)),
            };

            foreach (var respuesta in respuestas)
            {
                Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
                Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
                Assert.DoesNotContain(cuenta.Correo, await respuesta.Content.ReadAsStringAsync());
            }
        }

        // Tampoco desde su propio club, con el identificador real de la persona del otro club.
        var desdeSuClub = new[]
        {
            await ajeno.PostAsync($"/api/clubes/{suClub}/ingresos/{enEspera.Id}/aprobacion", new { rol = "JUGADOR" }),
            await ajeno.PostAsync($"/api/clubes/{suClub}/ingresos/{enEspera.Id}/rechazo"),
        };
        Assert.All(desdeSuClub, respuesta => Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode));
        Assert.DoesNotContain(
            cuenta.Correo, await (await ajeno.GetAsync($"/api/clubes/{suClub}/ingresos/en-espera")).Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            aprobado.Id.ToString(), await (await ajeno.GetAsync($"/api/clubes/{suClub}/ingresos/aprobados")).Content.ReadAsStringAsync());

        var sigue = (await _fabrica.IntegranteAsync(enEspera.Id))!;
        Assert.Equal(EstadoIngreso.EN_ESPERA, sigue.EstadoIngreso);
        Assert.Null(sigue.AprobadoEn);
        Assert.True(await _fabrica.ExisteCuentaAsync(cuenta.Id));
    }

    private async Task<ClienteDePrueba> PresidenteDeOtroClubAsync()
    {
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.PRESIDENTE);
        return await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
    }

    private static async Task<Guid> ClubDeAsync(ClienteDePrueba cliente)
    {
        var sesion = await ClienteDePrueba.LeerAsync<System.Text.Json.JsonElement>(await cliente.GetAsync("/api/sesion"));
        return sesion.GetProperty("clubes")[0].GetProperty("clubId").GetGuid();
    }

    private Task<Invitacion> InvitacionAsync(Guid invitacionId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Invitaciones.IgnoreQueryFilters().AsNoTracking().SingleAsync(invitacion => invitacion.Id == invitacionId));
}
