using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Dentro del club solo el PRESIDENTE envía, ve, reenvía y cancela invitaciones, ve la sala de
/// espera y los ingresos aprobados y aprueba o rechaza un ingreso: en esos ocho endpoints un
/// DIRECTIVO, un ENTRENADOR y un JUGADOR reciben 403 también llamando directamente a la API
/// (constitución §8 y §20; RF-002, RF-003 y RF-017; escenarios 1.3 y 3.7; CE-003).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SoloPresidenteIngresosPruebas
{
    private readonly FabricaApi _fabrica;

    public SoloPresidenteIngresosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public async Task Quien_no_es_presidente_recibe_403_en_los_cuatro_endpoints_de_invitaciones_y_no_cambia_nada(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var presidente = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);
        var invitado = Sembrador.CorreoUnico();
        var pendiente = await InvitarAsync(presidente, club, invitado, Rol.ENTRENADOR);
        var token = _fabrica.Correo.UltimoToken("invitacion", invitado);
        var cliente = await _fabrica.ClienteDeAsync(club, rol);
        var correoNuevo = Sembrador.CorreoUnico();

        var respuestas = new[]
        {
            await cliente.GetAsync(EscenarioIngresos.Invitaciones(club)),
            await cliente.InvitarAsync(club, correoNuevo, Rol.JUGADOR),
            await cliente.PostAsync(EscenarioIngresos.Reenvio(club, pendiente)),
            await cliente.PostAsync(EscenarioIngresos.Cancelacion(club, pendiente)),
        };

        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(invitado, await respuesta.Content.ReadAsStringAsync());
        }

        // No se envió ningún correo y la invitación sigue pendiente, con su enlace y su rol.
        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", correoNuevo));
        Assert.Equal(1, _fabrica.Correo.Contar("invitacion", invitado));
        var guardada = await InvitacionAsync(pendiente);
        Assert.Null(guardada.AnuladaEn);
        Assert.Equal(Rol.ENTRENADOR, guardada.Rol);
        Assert.Equal(
            HttpStatusCode.OK,
            (await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/consulta", new { token })).StatusCode);
        Assert.Single(await presidente.ListaAsync(EscenarioIngresos.Invitaciones(club)));
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public async Task Quien_no_es_presidente_recibe_403_en_los_cuatro_endpoints_de_ingresos_y_no_cambia_nada(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var presidente = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);
        var (cuenta, enEspera) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var (_, yaAprobado) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        await presidente.PostAsync(EscenarioIngresos.Aprobacion(club, yaAprobado.Id));
        var cliente = await _fabrica.ClienteDeAsync(club, rol);

        var respuestas = new[]
        {
            await cliente.GetAsync(EscenarioIngresos.EnEspera(club)),
            await cliente.GetAsync(EscenarioIngresos.Aprobados(club)),
            await cliente.PostAsync(EscenarioIngresos.Aprobacion(club, enEspera.Id)),
            await cliente.PostAsync(EscenarioIngresos.Rechazo(club, enEspera.Id)),
        };

        foreach (var respuesta in respuestas)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(cuenta.Correo, cuerpo);
            Assert.DoesNotContain(enEspera.NumeroDocumento, cuerpo);
            Assert.DoesNotContain(yaAprobado.Id.ToString(), cuerpo);
        }

        // La persona sigue en espera, sin aprobar ni borrar.
        var sigue = (await _fabrica.IntegranteAsync(enEspera.Id))!;
        Assert.Equal(EstadoIngreso.EN_ESPERA, sigue.EstadoIngreso);
        Assert.Null(sigue.AprobadoEn);
        Assert.True(await _fabrica.ExisteCuentaAsync(cuenta.Id));
        Assert.Single(await presidente.ListaAsync(EscenarioIngresos.EnEspera(club)));
        Assert.Single(await presidente.ListaAsync(EscenarioIngresos.Aprobados(club)));
    }

    [Fact]
    public async Task En_un_club_con_dos_presidentes_uno_ve_reenvia_y_cancela_la_invitacion_que_envio_el_otro()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (quienInvita, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var delQueInvita = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(quienInvita);
        var delOtro = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);
        var correo = Sembrador.CorreoUnico();
        var pendiente = await InvitarAsync(delQueInvita, club, correo, Rol.DIRECTIVO);

        var fila = Assert.Single(await delOtro.ListaAsync(EscenarioIngresos.Invitaciones(club)));
        var reenviar = await delOtro.PostAsync(EscenarioIngresos.Reenvio(club, pendiente));
        var nueva = await ClienteDePrueba.LeerAsync<JsonElement>(reenviar);
        var cancelar = await delOtro.PostAsync(
            EscenarioIngresos.Cancelacion(club, nueva.GetProperty("invitacionId").GetGuid()));

        Assert.Equal(pendiente, fila.GetProperty("invitacionId").GetGuid());
        Assert.Equal($"{integrante.Nombres} {integrante.Apellidos}", fila.GetProperty("enviadaPor").GetString());
        Assert.Equal(HttpStatusCode.Created, reenviar.StatusCode);
        Assert.Equal("DIRECTIVO", nueva.GetProperty("rol").GetString());
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);
        Assert.Equal("CANCELADA", (await ClienteDePrueba.LeerAsync<JsonElement>(cancelar)).GetProperty("estado").GetString());
    }

    private static async Task<Guid> InvitarAsync(ClienteDePrueba cliente, Club club, string correo, Rol rol)
    {
        var respuesta = await cliente.InvitarAsync(club, correo, rol);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("invitacionId").GetGuid();
    }

    private Task<Invitacion> InvitacionAsync(Guid invitacionId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Invitaciones.IgnoreQueryFilters().AsNoTracking().SingleAsync(invitacion => invitacion.Id == invitacionId));
}
