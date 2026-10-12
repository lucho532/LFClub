using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Aislamiento;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Ingresos en un club suspendido y en uno dado de baja (RF-021): suspendido, solo su PRESIDENTE
/// opera, con sus tres roles, y los enlaces siguen sirviendo; dado de baja, nada funciona hasta
/// revertir la baja.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class IngresosPorEstadoDelClubPruebas
{
    private readonly FabricaApi _fabrica;

    public IngresosPorEstadoDelClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private List<Endpoint> DeIngresos => EndpointsDeLaApi.DeLaApi(_fabrica)
        .Where(endpoint => endpoint.Ruta.StartsWith("/api/clubes/{clubId}/invitaciones", StringComparison.Ordinal)
            || endpoint.Ruta.StartsWith("/api/clubes/{clubId}/ingresos", StringComparison.Ordinal))
        .ToList();

    [Fact]
    public async Task En_un_club_suspendido_el_presidente_invita_reenvia_cancela_aprueba_y_rechaza()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        var presidente = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);
        var (_, paraAprobar) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var (_, paraRechazar) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var invitaciones = $"/api/clubes/{club.Id}/invitaciones";

        // Suspendido, el presidente sigue invitando con cualquiera de sus tres roles (RF-021).
        foreach (var rol in new[] { Rol.ENTRENADOR, Rol.DIRECTIVO })
        {
            var conOtroRol = await presidente.InvitarAsync(club, Sembrador.CorreoUnico(), rol);
            Assert.Equal(HttpStatusCode.Created, conOtroRol.StatusCode);
        }

        var invitar = await presidente.PostAsync(invitaciones, new { correo = Sembrador.CorreoUnico(), rol = "JUGADOR" });
        var invitacionId = (await ClienteDePrueba.LeerAsync<JsonElement>(invitar)).GetProperty("invitacionId").GetGuid();
        var reenviar = await presidente.PostAsync($"{invitaciones}/{invitacionId}/reenvio");
        var nuevaId = (await ClienteDePrueba.LeerAsync<JsonElement>(reenviar)).GetProperty("invitacionId").GetGuid();
        var cancelar = await presidente.PostAsync($"{invitaciones}/{nuevaId}/cancelacion");
        var aprobar = await presidente.PostAsync(EscenarioIngresos.Aprobacion(club, paraAprobar.Id));
        var rechazar = await presidente.PostAsync(EscenarioIngresos.Rechazo(club, paraRechazar.Id));

        Assert.Equal(HttpStatusCode.Created, invitar.StatusCode);
        Assert.Equal(HttpStatusCode.Created, reenviar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aprobar.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, rechazar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await presidente.GetAsync(invitaciones)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await presidente.GetAsync(EscenarioIngresos.EnEspera(club))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await presidente.GetAsync(EscenarioIngresos.Aprobados(club))).StatusCode);
    }

    [Theory]
    [InlineData(EstadoClub.SUSPENDIDO, Rol.DIRECTIVO, "club_suspendido")]
    [InlineData(EstadoClub.DADO_DE_BAJA, Rol.DIRECTIVO, "club_dado_de_baja")]
    [InlineData(EstadoClub.DADO_DE_BAJA, Rol.PRESIDENTE, "club_dado_de_baja")]
    public async Task Quien_no_entra_al_club_recibe_403_en_los_ocho_endpoints_y_no_cambia_nada(
        EstadoClub estado, Rol rol, string codigo)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: estado);
        var cliente = await _fabrica.ClienteDeAsync(club, rol);
        var (_, enEspera) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        Assert.Equal(8, DeIngresos.Count);

        foreach (var endpoint in DeIngresos)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(cliente, endpoint, club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
        }

        // Con los identificadores reales tampoco: ni aprueba ni rechaza ni invita.
        var reales = new[]
        {
            await cliente.PostAsync(EscenarioIngresos.Aprobacion(club, enEspera.Id), new { rol = "JUGADOR" }),
            await cliente.PostAsync(EscenarioIngresos.Rechazo(club, enEspera.Id)),
            await cliente.PostAsync($"/api/clubes/{club.Id}/invitaciones", new { correo = Sembrador.CorreoUnico(), rol = "JUGADOR" }),
        };
        foreach (var respuesta in reales)
        {
            Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Equal(EstadoIngreso.EN_ESPERA, (await _fabrica.IntegranteAsync(enEspera.Id))!.EstadoIngreso);
    }

    [Fact]
    public async Task En_un_club_suspendido_quien_se_registra_queda_aprobado_ve_el_aviso_y_entra_al_levantar_la_suspension()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        var venceEn = DateTime.UtcNow.AddDays(2);
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, venceEn: venceEn, rol: Rol.ENTRENADOR);
        var cliente = _fabrica.CrearClienteDePrueba();

        var consulta = await cliente.PostAsync("/api/invitaciones/consulta", new { token });
        var registro = await cliente.PostAsync("/api/invitaciones/registro", Datos(token));

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        cliente.UsarToken((await ClienteDePrueba.LeerAsync<JsonElement>(registro)).GetProperty("token").GetString()!);
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        var unico = Assert.Single(sesion.GetProperty("clubes").EnumerateArray());
        Assert.Equal("APROBADO", unico.GetProperty("estadoIngreso").GetString());
        Assert.Equal("ENTRENADOR", unico.GetProperty("rol").GetString());
        // Como cualquier integrante que no es el presidente, ve el aviso de incidencia temporal (RF-021).
        var suspendido = await cliente.GetAsync($"/api/clubes/{club.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, suspendido.StatusCode);
        Assert.Equal("club_suspendido", await ClienteDePrueba.CodigoAsync(suspendido));
        // La suspensión no alarga el plazo de la invitación.
        Assert.Equal(invitacion.VenceEn, (await InvitacionAsync(invitacion.Id)).VenceEn, TimeSpan.FromMilliseconds(1));

        // Al levantar la suspensión entra con su rol, sin pasar por ninguna aprobación.
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await desarrollador.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "ACTIVO" });
        var activo = await cliente.GetAsync($"/api/clubes/{club.Id}");
        Assert.Equal(HttpStatusCode.OK, activo.StatusCode);
        Assert.Equal("ENTRENADOR", (await ClienteDePrueba.LeerAsync<JsonElement>(activo)).GetProperty("miRol").GetString());
    }

    [Fact]
    public async Task En_un_club_dado_de_baja_el_enlace_no_sirve_y_vuelve_a_servir_al_revertir_la_baja()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (conCuenta, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.JUGADOR);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.JUGADOR);
        var (_, paraAceptar) = await _fabrica.Sembrador.CrearInvitacionAsync(club, conCuenta.Correo, rol: Rol.JUGADOR);
        var (_, dePresidente) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var suCliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(conCuenta);
        var anonimo = _fabrica.CrearClienteDePrueba();
        var estado = $"/api/plataforma/clubes/{club.Id}/estado";

        await desarrollador.PutAsync(estado, new { estado = "DADO_DE_BAJA" });
        var respuestas = new[]
        {
            await anonimo.PostAsync("/api/invitaciones/consulta", new { token }),
            await anonimo.PostAsync("/api/invitaciones/registro", Datos(token)),
            await suCliente.PostAsync("/api/invitaciones/aceptacion", new { token = paraAceptar }),
        };
        // La invitación de presidente no cambia de comportamiento (supuesto 4).
        var consultaDePresidente = await anonimo.PostAsync("/api/invitaciones/consulta", new { token = dePresidente });

        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.Gone, respuesta.StatusCode);
            Assert.Equal("invitacion_no_valida", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Equal(HttpStatusCode.OK, consultaDePresidente.StatusCode);

        await desarrollador.PutAsync(estado, new { estado = "ACTIVO" });
        Assert.Equal(HttpStatusCode.OK, (await anonimo.PostAsync("/api/invitaciones/consulta", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await anonimo.PostAsync("/api/invitaciones/registro", Datos(token))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await suCliente.PostAsync("/api/invitaciones/aceptacion", new { token = paraAceptar })).StatusCode);
    }

    private static object Datos(string token) => EscenarioIngresos.DatosDeRegistro(token);

    private Task<Invitacion> InvitacionAsync(Guid invitacionId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Invitaciones.IgnoreQueryFilters().AsNoTracking().SingleAsync(invitacion => invitacion.Id == invitacionId));
}
