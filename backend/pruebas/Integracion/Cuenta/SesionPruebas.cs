using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

[Collection(ColeccionApi.Nombre)]
public class SesionPruebas
{
    private readonly FabricaApi _fabrica;

    public SesionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Entra_con_el_correo_y_con_el_documento()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var integrante = await _fabrica.Sembrador.CrearIntegranteAsync(club, usuario, Rol.PRESIDENTE);
        var cliente = _fabrica.CrearClienteDePrueba();

        var conCorreo = await cliente.IniciarSesionAsync(usuario.Correo);
        var conDocumento = await cliente.IniciarSesionAsync(integrante.NumeroDocumento);

        Assert.Equal(HttpStatusCode.OK, conCorreo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, conDocumento.StatusCode);
        var token = await ClienteDePrueba.LeerAsync<JsonElement>(conDocumento);
        Assert.True(token.GetProperty("venceEn").GetDateTime() > DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task Entra_igual_con_mayusculas_y_espacios_en_el_correo_y_en_el_documento()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var integrante = await _fabrica.Sembrador.CrearIntegranteAsync(club, usuario, Rol.DIRECTIVO, "AB.12 " + Sembrador.Unico());
        var cliente = _fabrica.CrearClienteDePrueba();

        var conCorreo = await cliente.IniciarSesionAsync($"  {usuario.Correo.ToUpperInvariant()}  ");
        var conDocumento = await cliente.IniciarSesionAsync($" {integrante.NumeroDocumento.ToUpperInvariant()} ");

        Assert.Equal(HttpStatusCode.OK, conCorreo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, conDocumento.StatusCode);
    }

    [Fact]
    public async Task La_contrasena_no_se_normaliza()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = _fabrica.CrearClienteDePrueba();

        var conEspacio = await cliente.IniciarSesionAsync(usuario.Correo, Sembrador.Contrasena + " ");
        var enMinusculas = await cliente.IniciarSesionAsync(usuario.Correo, Sembrador.Contrasena.ToLowerInvariant());

        Assert.Equal(HttpStatusCode.Unauthorized, conEspacio.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, enMinusculas.StatusCode);
    }

    [Fact]
    public async Task Cuenta_inexistente_contrasena_incorrecta_y_cuenta_bloqueada_responden_lo_mismo()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var bloqueada = await _fabrica.Sembrador.CrearCuentaAsync();
        var sinContrasena = await _fabrica.Sembrador.CrearCuentaAsync(contrasena: null);
        await _fabrica.ConContextoAsync(contexto => contexto.Usuarios
            .Where(cuenta => cuenta.Id == bloqueada.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(cuenta => cuenta.Bloqueada, true)));
        var cliente = _fabrica.CrearClienteDePrueba();

        var respuestas = new[]
        {
            await cliente.IniciarSesionAsync(Sembrador.CorreoUnico()),
            await cliente.IniciarSesionAsync("documento-que-no-existe"),
            await cliente.IniciarSesionAsync(usuario.Correo, "otra-contrasena"),
            await cliente.IniciarSesionAsync(bloqueada.Correo),
            await cliente.IniciarSesionAsync(sinContrasena.Correo, string.Empty),
            await cliente.IniciarSesionAsync(string.Empty, string.Empty),
        };

        var cuerpos = new List<string>();
        foreach (var respuesta in respuestas)
        {
            await respuesta.Content.LoadIntoBufferAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            Assert.Equal("credenciales_invalidas", await ClienteDePrueba.CodigoAsync(respuesta));
            cuerpos.Add(await respuesta.Content.ReadAsStringAsync());
        }

        Assert.Single(cuerpos.Distinct());
        Assert.Contains("Olvidé mi contraseña", await ClienteDePrueba.TituloAsync(respuestas[0]));
    }

    [Fact]
    public async Task La_sesion_devuelve_los_clubes_de_la_cuenta_y_nunca_la_contrasena()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, usuario, Rol.ENTRENADOR, nombres: "Lucía");
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.GetAsync("/api/sesion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("contrasena", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sello", texto, StringComparison.OrdinalIgnoreCase);

        var sesion = JsonDocument.Parse(texto).RootElement;
        Assert.Equal(usuario.Id, sesion.GetProperty("usuarioId").GetGuid());
        Assert.False(sesion.GetProperty("esDesarrollador").GetBoolean());
        var unico = Assert.Single(sesion.GetProperty("clubes").EnumerateArray());
        Assert.Equal(club.Id, unico.GetProperty("clubId").GetGuid());
        Assert.Equal("ENTRENADOR", unico.GetProperty("rol").GetString());
        Assert.Equal("ACTIVO", unico.GetProperty("estado").GetString());
        Assert.Equal("Lucía", unico.GetProperty("nombres").GetString());
        Assert.Equal(JsonValueKind.Null, unico.GetProperty("identidad").GetProperty("urlEscudo").ValueKind);
    }

    [Fact]
    public async Task La_sesion_de_quien_esta_aprobado_en_un_club_y_en_espera_en_otro_trae_los_dos_con_su_estado_de_ingreso()
    {
        var aprobado = await _fabrica.Sembrador.CrearClubAsync();
        var enEspera = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var documento = Sembrador.Unico("doc");
        await _fabrica.Sembrador.CrearIntegranteAsync(aprobado, usuario, Rol.ENTRENADOR, documento);
        await _fabrica.Sembrador.CrearIntegranteAsync(
            enEspera, usuario, Rol.JUGADOR, documento, estadoIngreso: EstadoIngreso.EN_ESPERA);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));

        var clubes = sesion.GetProperty("clubes").EnumerateArray().ToDictionary(c => c.GetProperty("clubId").GetGuid());
        Assert.Equal(2, clubes.Count);
        Assert.Equal("APROBADO", clubes[aprobado.Id].GetProperty("estadoIngreso").GetString());
        Assert.Equal("EN_ESPERA", clubes[enEspera.Id].GetProperty("estadoIngreso").GetString());
        // El club en espera trae su nombre y su identidad: con eso se pinta la sala de espera (RF-018).
        Assert.Equal(enEspera.Nombre, clubes[enEspera.Id].GetProperty("nombre").GetString());
        Assert.Equal(JsonValueKind.Object, clubes[enEspera.Id].GetProperty("identidad").ValueKind);
        Assert.Equal(aprobado.Nombre, clubes[aprobado.Id].GetProperty("nombre").GetString());
        Assert.Equal(JsonValueKind.Object, clubes[aprobado.Id].GetProperty("identidad").ValueKind);
    }

    [Fact]
    public async Task La_sesion_del_desarrollador_no_tiene_clubes()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));

        Assert.True(sesion.GetProperty("esDesarrollador").GetBoolean());
        Assert.Empty(sesion.GetProperty("clubes").EnumerateArray());
    }

    [Fact]
    public async Task Sin_token_o_con_un_token_inventado_responde_401_sin_sesion()
    {
        var cliente = _fabrica.CrearClienteDePrueba();

        var sinToken = await cliente.GetAsync("/api/sesion");
        cliente.UsarToken("esto-no-es-un-token");
        var inventado = await cliente.GetAsync("/api/sesion");

        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(sinToken));
        Assert.Equal(HttpStatusCode.Unauthorized, inventado.StatusCode);
        Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(inventado));
    }

    [Fact]
    public async Task Un_token_con_el_sello_antiguo_responde_401()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/sesion")).StatusCode);

        await _fabrica.ConContextoAsync(contexto => contexto.Usuarios
            .Where(cuenta => cuenta.Id == usuario.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(cuenta => cuenta.SelloSeguridad, Guid.NewGuid())));

        var respuesta = await cliente.GetAsync("/api/sesion");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task La_renovacion_entrega_un_token_nuevo_que_sirve()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var anterior = cliente.Token;

        var respuesta = await cliente.PostAsync("/api/sesion/renovacion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var nuevo = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("token").GetString()!;
        Assert.NotEqual(anterior, nuevo);
        cliente.UsarToken(nuevo);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/sesion")).StatusCode);
    }

    [Fact]
    public async Task Renovar_sin_sesion_responde_401()
    {
        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync("/api/sesion/renovacion");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
