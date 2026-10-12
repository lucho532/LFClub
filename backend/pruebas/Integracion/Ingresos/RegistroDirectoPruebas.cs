using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Quien se registra con una invitación del club entra directamente con el rol de la invitación,
/// aprobado y sin sala de espera, y una invitación no sirve dos veces, vencida ni cancelada
/// (constitución §12.1 y §20; RF-008, RF-009, RF-014 y RF-019; historia 2). La ubicación del
/// jugador en su categoría está en <see cref="RegistroDirectoUbicacionPruebas"/> y el nombre del
/// responsable, en <see cref="RegistroDirectoResponsablePruebas"/>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RegistroDirectoPruebas
{
    private const string Registro = "/api/invitaciones/registro";
    private const string Consulta = "/api/invitaciones/consulta";

    private readonly FabricaApi _fabrica;

    public RegistroDirectoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public async Task Queda_aprobado_con_ese_unico_rol_y_entra_al_club_sin_que_nadie_lo_apruebe(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var presidente = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);

        var (cliente, integrante) = await _fabrica.RegistrarConInvitacionAsync(club, rol);

        Assert.Equal(rol, integrante.Rol);
        Assert.Equal(EstadoIngreso.APROBADO, integrante.EstadoIngreso);
        Assert.Null(integrante.AprobadoEn);
        Assert.Null(integrante.AprobadoPorUsuarioId);
        Assert.Null(integrante.AprobadoPorNombre);
        Assert.Null(integrante.RolDeIngreso);
        Assert.Equal(1, await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().CountAsync(fila => fila.UsuarioId == integrante.UsuarioId)));

        // Con el token del registro ve su club y entra a él de inmediato (CE-001 y CE-002).
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        var unico = Assert.Single(sesion.GetProperty("clubes").EnumerateArray());
        Assert.Equal("APROBADO", unico.GetProperty("estadoIngreso").GetString());
        Assert.Equal(rol.ToString(), unico.GetProperty("rol").GetString());
        var suClub = await cliente.GetAsync($"/api/clubes/{club.Id}");
        Assert.Equal(HttpStatusCode.OK, suClub.StatusCode);
        Assert.Equal(rol.ToString(), (await ClienteDePrueba.LeerAsync<JsonElement>(suClub)).GetProperty("miRol").GetString());

        // Ni sala de espera ni aprobación: el club lo ve como invitación usada, con su rol (RF-019).
        Assert.Empty(await presidente.ListaAsync(EscenarioIngresos.EnEspera(club)));
        Assert.Empty(await presidente.ListaAsync(EscenarioIngresos.Aprobados(club)));
        var invitacion = Assert.Single(await presidente.ListaAsync(EscenarioIngresos.Invitaciones(club)));
        Assert.Equal("USADA", invitacion.GetProperty("estado").GetString());
        Assert.Equal(rol.ToString(), invitacion.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task Entra_con_el_correo_y_con_el_documento_y_la_respuesta_del_registro_no_trae_datos_del_club()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.ENTRENADOR);
        var documento = Sembrador.Unico("doc");

        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync(Registro, EscenarioIngresos.DatosDeRegistro(token, documento));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.DoesNotContain(club.Nombre, await respuesta.Content.ReadAsStringAsync());
        foreach (var identificador in new[] { invitacion.Correo, documento })
        {
            var otro = _fabrica.CrearClienteDePrueba();
            var entrar = await otro.IniciarSesionAsync(identificador, EscenarioIngresos.ContrasenaDeRegistro);
            Assert.Equal(HttpStatusCode.OK, entrar.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await otro.GetAsync($"/api/clubes/{club.Id}")).StatusCode);
        }
    }

    [Fact]
    public async Task Sin_token_no_hay_registro_y_una_invitacion_usada_vencida_cancelada_o_reemplazada_responde_410()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var presidente = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);
        var (_, usada) = await _fabrica.Sembrador.CrearInvitacionAsync(club, usadaEn: DateTime.UtcNow, rol: Rol.JUGADOR);
        var (_, vencida) = await _fabrica.Sembrador.CrearInvitacionAsync(club, venceEn: DateTime.UtcNow.AddSeconds(-1), rol: Rol.DIRECTIVO);
        var (porCancelar, cancelada) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.ENTRENADOR);
        var (porReemplazar, reemplazada) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.ENTRENADOR);
        await presidente.PostAsync(EscenarioIngresos.Cancelacion(club, porCancelar.Id));
        await presidente.InvitarAsync(club, porReemplazar.Correo, Rol.DIRECTIVO);
        var cliente = _fabrica.CrearClienteDePrueba();
        var antes = await ContarIntegrantesAsync(club.Id);

        foreach (var token in new[] { usada, vencida, cancelada, reemplazada, "no-existe", "" })
        {
            var consulta = await cliente.PostAsync(Consulta, new { token });
            var registro = await cliente.PostAsync(Registro, EscenarioIngresos.DatosDeRegistro(token));

            Assert.Equal(HttpStatusCode.Gone, consulta.StatusCode);
            Assert.Equal(HttpStatusCode.Gone, registro.StatusCode);
            Assert.Equal("invitacion_no_valida", await ClienteDePrueba.CodigoAsync(registro));
        }

        Assert.Equal(antes, await ContarIntegrantesAsync(club.Id));
    }

    [Fact]
    public async Task Un_rol_un_correo_o_un_club_enviados_en_el_cuerpo_se_ignoran()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.JUGADOR);
        var documento = Sembrador.Unico("doc");
        var cuerpo = JsonSerializer.Deserialize<JsonElement>(
            $"{{\"token\":\"{token}\",\"correo\":\"intruso@lfclub.test\",\"rol\":\"PRESIDENTE\",\"estadoIngreso\":\"EN_ESPERA\"," +
            $"\"clubId\":\"{otroClub.Id}\",\"nombres\":\"Ana\",\"apellidos\":\"Pérez\",\"tipoDocumento\":\"CEDULA_CIUDADANIA\"," +
            $"\"numeroDocumento\":\"{documento}\",\"fechaNacimiento\":\"1990-01-01\",\"celular\":\"3001234567\"," +
            $"\"contrasena\":\"{EscenarioIngresos.ContrasenaDeRegistro}\"}}");

        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync(Registro, cuerpo);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var integrante = await IntegranteAsync(club.Id, documento);
        Assert.Equal(Rol.JUGADOR, integrante.Rol);
        Assert.Equal(EstadoIngreso.APROBADO, integrante.EstadoIngreso);
        Assert.Equal(invitacion.Correo, (await _fabrica.CuentaAsync(integrante.UsuarioId)).CorreoNormalizado);
        Assert.Equal(0, await ContarIntegrantesAsync(otroClub.Id));
        Assert.False(await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.AnyAsync(u => u.CorreoNormalizado == "intruso@lfclub.test")));
    }

    [Theory]
    [InlineData(Rol.JUGADOR, true)]
    [InlineData(Rol.ENTRENADOR, false)]
    [InlineData(Rol.DIRECTIVO, false)]
    [InlineData(Rol.PRESIDENTE, false)]
    public async Task La_consulta_dice_el_rol_y_pide_el_responsable_solo_al_jugador(Rol rol, bool pideResponsable)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: rol);

        var consulta = await ClienteDePrueba.LeerAsync<JsonElement>(
            await _fabrica.CrearClienteDePrueba().PostAsync(Consulta, new { token }));

        Assert.Equal(rol.ToString(), consulta.GetProperty("rol").GetString());
        Assert.Equal(pideResponsable, consulta.GetProperty("pideResponsable").GetBoolean());
        Assert.False(consulta.TryGetProperty("pasaPorSalaDeEspera", out _));
    }

    [Fact]
    public async Task El_documento_repetido_en_el_club_o_de_otra_cuenta_responde_409()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (_, enEspera) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var (_, deOtroClub) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.JUGADOR);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.ENTRENADOR);
        var cliente = _fabrica.CrearClienteDePrueba();

        var repetido = await cliente.PostAsync(Registro, EscenarioIngresos.DatosDeRegistro(token, enEspera.NumeroDocumento));
        var deOtraCuenta = await cliente.PostAsync(Registro, EscenarioIngresos.DatosDeRegistro(token, deOtroClub.NumeroDocumento));

        Assert.Equal("documento_repetido_en_club", await ClienteDePrueba.CodigoAsync(repetido));
        Assert.Equal("documento_en_otra_cuenta", await ClienteDePrueba.CodigoAsync(deOtraCuenta));
    }

    private Task<UsuarioRol> IntegranteAsync(Guid clubId, string documento) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(i => i.ClubId == clubId && i.NumeroDocumento == documento));

    private Task<int> ContarIntegrantesAsync(Guid clubId) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().CountAsync(i => i.ClubId == clubId));
}
