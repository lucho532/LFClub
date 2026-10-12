using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// No es posible registrarse sin una invitación válida, y la cuenta queda en el club de la
/// invitación (constitución §20, RF-013 a RF-017).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RegistroConInvitacionPruebas
{
    private readonly FabricaApi _fabrica;

    public RegistroConInvitacionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_consulta_devuelve_el_club_el_rol_y_el_correo_de_la_invitacion()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);

        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/consulta", new { token });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var dto = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(club.Nombre, dto.GetProperty("nombreClub").GetString());
        Assert.Equal("PRESIDENTE", dto.GetProperty("rol").GetString());
        Assert.Equal(invitacion.Correo, dto.GetProperty("correo").GetString());
        Assert.False(dto.GetProperty("tieneCuenta").GetBoolean());
        Assert.DoesNotContain(token, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_registro_deja_a_la_persona_como_presidente_aprobado_del_club_de_la_invitacion()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var documento = Sembrador.Unico("doc");
        var cliente = _fabrica.CrearClienteDePrueba();

        var respuesta = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, $" {documento.ToUpperInvariant()[..3]}.{documento[3..]} "));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        cliente.UsarToken((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("token").GetString()!);

        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        Assert.Equal(invitacion.Correo, sesion.GetProperty("correo").GetString());
        Assert.False(sesion.GetProperty("esDesarrollador").GetBoolean());
        var unico = Assert.Single(sesion.GetProperty("clubes").EnumerateArray());
        Assert.Equal(club.Id, unico.GetProperty("clubId").GetGuid());
        Assert.Equal("PRESIDENTE", unico.GetProperty("rol").GetString());
        Assert.Equal("Ana María", unico.GetProperty("nombres").GetString());

        var integrante = await IntegranteAsync(club.Id, documento);
        Assert.Equal(EstadoIngreso.APROBADO, integrante.EstadoIngreso);
        Assert.Equal(documento, integrante.NumeroDocumento);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/clubes/{club.Id}")).StatusCode);

        // Puede entrar con el correo y con el documento, con la contraseña que creó.
        var otro = _fabrica.CrearClienteDePrueba();
        Assert.Equal(HttpStatusCode.OK, (await otro.IniciarSesionAsync(invitacion.Correo, "mi-contrasena-propia")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await otro.IniciarSesionAsync(documento, "mi-contrasena-propia")).StatusCode);
    }

    [Fact]
    public async Task Una_invitacion_usada_vencida_reemplazada_o_inexistente_responde_410()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, usada) = await _fabrica.Sembrador.CrearInvitacionAsync(club, usadaEn: DateTime.UtcNow);
        var (_, vencida) = await _fabrica.Sembrador.CrearInvitacionAsync(club, venceEn: DateTime.UtcNow.AddSeconds(-1));
        var (_, anulada) = await _fabrica.Sembrador.CrearInvitacionAsync(club, anuladaEn: DateTime.UtcNow);
        var cliente = _fabrica.CrearClienteDePrueba();

        foreach (var token in new[] { usada, vencida, anulada, "no-existe", "" })
        {
            var consulta = await cliente.PostAsync("/api/invitaciones/consulta", new { token });
            var registro = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, Sembrador.Unico("doc")));

            Assert.Equal(HttpStatusCode.Gone, consulta.StatusCode);
            Assert.Equal("invitacion_no_valida", await ClienteDePrueba.CodigoAsync(consulta));
            Assert.Equal(HttpStatusCode.Gone, registro.StatusCode);
            Assert.Equal("invitacion_no_valida", await ClienteDePrueba.CodigoAsync(registro));
        }
    }

    [Fact]
    public async Task Sin_token_no_hay_forma_de_registrarse_y_la_invitacion_sirve_una_sola_vez()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = _fabrica.CrearClienteDePrueba();
        var sinToken = JsonSerializer.Deserialize<JsonElement>(
            "{\"nombres\":\"Ana\",\"apellidos\":\"Pérez\",\"tipoDocumento\":\"CEDULA_CIUDADANIA\",\"numeroDocumento\":\"999\"," +
            "\"fechaNacimiento\":\"1990-01-01\",\"celular\":\"300\",\"contrasena\":\"mi-contrasena-propia\",\"correo\":\"x@y.co\"}");

        var sin = await cliente.PostAsync("/api/invitaciones/registro", sinToken);
        var primera = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, Sembrador.Unico("doc")));
        var segunda = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, Sembrador.Unico("doc")));

        Assert.Equal(HttpStatusCode.Gone, sin.StatusCode);
        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, segunda.StatusCode);
    }

    [Fact]
    public async Task Un_correo_o_una_marca_de_desarrollador_enviados_en_el_cuerpo_se_ignoran()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var documento = Sembrador.Unico("doc");
        var cuerpo = JsonSerializer.Deserialize<JsonElement>(
            $"{{\"token\":\"{token}\",\"correo\":\"intruso@lfclub.test\",\"esDesarrollador\":true,\"rol\":\"JUGADOR\"," +
            $"\"clubId\":\"{Guid.NewGuid()}\",\"nombres\":\"Ana\",\"apellidos\":\"Pérez\",\"tipoDocumento\":\"CEDULA_CIUDADANIA\"," +
            $"\"numeroDocumento\":\"{documento}\",\"fechaNacimiento\":\"1990-01-01\",\"celular\":\"3001234567\",\"contrasena\":\"mi-contrasena-propia\"}}");

        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/registro", cuerpo);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var integrante = await IntegranteAsync(club.Id, documento);
        var cuenta = await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.SingleAsync(u => u.Id == integrante.UsuarioId));
        Assert.Equal(invitacion.Correo, cuenta.CorreoNormalizado);
        Assert.False(cuenta.EsDesarrollador);
        Assert.Equal(Rol.PRESIDENTE, integrante.Rol);
        Assert.False(await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.AnyAsync(u => u.CorreoNormalizado == "intruso@lfclub.test")));
        Assert.Equal(1, await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.CountAsync(u => u.EsDesarrollador)));
    }

    [Fact]
    public async Task Si_el_correo_ya_tiene_cuenta_responde_409_y_la_consulta_lo_avisa()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var existente = await _fabrica.Sembrador.CrearCuentaAsync();
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, existente.Correo);
        var cliente = _fabrica.CrearClienteDePrueba();

        var consulta = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.PostAsync("/api/invitaciones/consulta", new { token }));
        var registro = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, Sembrador.Unico("doc")));

        Assert.True(consulta.GetProperty("tieneCuenta").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, registro.StatusCode);
        Assert.Equal("correo_ya_registrado", await ClienteDePrueba.CodigoAsync(registro));
    }

    [Fact]
    public async Task El_mismo_documento_no_se_repite_en_un_club_ni_puede_estar_en_dos_cuentas()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (_, deEsteClub) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var (_, deOtroClub) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.DIRECTIVO);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = _fabrica.CrearClienteDePrueba();

        var repetido = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, deEsteClub.NumeroDocumento.ToUpperInvariant()));
        var deOtraCuenta = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, deOtroClub.NumeroDocumento));
        var valido = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, Sembrador.Unico("doc")));

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal("documento_repetido_en_club", await ClienteDePrueba.CodigoAsync(repetido));
        Assert.Equal(HttpStatusCode.Conflict, deOtraCuenta.StatusCode);
        Assert.Equal("documento_en_otra_cuenta", await ClienteDePrueba.CodigoAsync(deOtraCuenta));
        // Ningún rechazo gastó la invitación.
        Assert.Equal(HttpStatusCode.Created, valido.StatusCode);
    }

    // Research §16 de la 006: basta con que el número tenga dueño, aunque esté en dos cuentas.
    [Fact]
    public async Task Un_documento_que_esta_en_dos_cuentas_tampoco_se_registra()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, enUna) = await _fabrica.Sembrador.CrearIntegranteAsync(await _fabrica.Sembrador.CrearClubAsync(), Rol.JUGADOR);
        await _fabrica.Sembrador.CrearIntegranteAsync(
            await _fabrica.Sembrador.CrearClubAsync(), await _fabrica.Sembrador.CrearCuentaAsync(), Rol.JUGADOR, enUna.NumeroDocumento);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);

        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/registro", Datos(token, enUna.NumeroDocumento));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("documento_en_otra_cuenta", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Los_datos_no_validos_responden_400_por_campo_y_no_gastan_la_invitacion()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = _fabrica.CrearClienteDePrueba();
        var manana = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)).ToString("yyyy-MM-dd");

        var respuesta = await cliente.PostAsync("/api/invitaciones/registro", new
        {
            token, nombres = " ", apellidos = new string('a', 81), numeroDocumento = ". .",
            fechaNacimiento = manana, celular = "", contrasena = "corta",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("errores");
        foreach (var campo in new[] { "nombres", "apellidos", "tipoDocumento", "numeroDocumento", "fechaNacimiento", "celular", "contrasena" })
        {
            Assert.True(errores.TryGetProperty(campo, out _), $"Falta el error de {campo}");
        }

        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsync("/api/invitaciones/registro", Datos(token, Sembrador.Unico("doc")))).StatusCode);
    }

    [Fact]
    public async Task Un_segundo_presidente_invitado_deja_el_club_con_dos()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club);

        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/registro", Datos(token, Sembrador.Unico("doc")));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var presidentes = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().CountAsync(i => i.ClubId == club.Id && i.Rol == Rol.PRESIDENTE));
        Assert.Equal(2, presidentes);
    }

    private static object Datos(string token, string numeroDocumento) => new
    {
        token,
        nombres = "  Ana   María ",
        apellidos = "Pérez Gómez",
        tipoDocumento = "CEDULA_CIUDADANIA",
        numeroDocumento,
        fechaNacimiento = "1988-03-15",
        celular = "3001234567",
        contrasena = "mi-contrasena-propia",
    };

    private Task<UsuarioRol> IntegranteAsync(Guid clubId, string documento) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(i => i.ClubId == clubId && i.NumeroDocumento == documento));
}
