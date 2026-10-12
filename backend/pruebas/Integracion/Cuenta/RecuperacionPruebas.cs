using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>Recuperación de contraseña (constitución §20, RF-005a).</summary>
[Collection(ColeccionApi.Nombre)]
public class RecuperacionPruebas
{
    private const string Nueva = "otra-contrasena-nueva";

    private readonly FabricaApi _fabrica;

    public RecuperacionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_desarrollador_sin_contrasena_crea_la_suya_y_entra()
    {
        var cliente = _fabrica.CrearClienteDePrueba();

        var pedir = await cliente.PostAsync("/api/cuenta/recuperacion", new { correo = $" {FabricaApi.CorreoDesarrollador.ToUpperInvariant()} " });
        var token = _fabrica.Correo.UltimoToken("recuperacion", FabricaApi.CorreoDesarrollador);
        var confirmar = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = Nueva });
        var entrar = await cliente.IniciarSesionAsync(FabricaApi.CorreoDesarrollador, Nueva);

        Assert.Equal(HttpStatusCode.Accepted, pedir.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, confirmar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, entrar.StatusCode);
    }

    [Fact]
    public async Task Un_correo_inexistente_responde_lo_mismo_y_no_envia_nada()
    {
        var correo = Sembrador.CorreoUnico();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = _fabrica.CrearClienteDePrueba();

        var inexistente = await cliente.PostAsync("/api/cuenta/recuperacion", new { correo });
        var existente = await cliente.PostAsync("/api/cuenta/recuperacion", new { correo = usuario.Correo });

        Assert.Equal(HttpStatusCode.Accepted, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, existente.StatusCode);
        Assert.Equal(await existente.Content.ReadAsStringAsync(), await inexistente.Content.ReadAsStringAsync());
        Assert.Equal(0, _fabrica.Correo.Contar("recuperacion", correo));
        Assert.Equal(1, _fabrica.Correo.Contar("recuperacion", usuario.Correo));
    }

    [Fact]
    public async Task El_enlace_sirve_una_sola_vez()
    {
        var (cliente, _, token) = await PedirAsync();

        var primera = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = Nueva });
        var segunda = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = "y-otra-distinta" });

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, segunda.StatusCode);
        Assert.Equal("enlace_no_valido", await ClienteDePrueba.CodigoAsync(segunda));
    }

    [Fact]
    public async Task Una_solicitud_nueva_anula_la_anterior()
    {
        var (cliente, correo, primero) = await PedirAsync();
        await cliente.PostAsync("/api/cuenta/recuperacion", new { correo });
        var segundo = _fabrica.Correo.UltimoToken("recuperacion", correo);

        var conElPrimero = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token = primero, contrasenaNueva = Nueva });
        var conElSegundo = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token = segundo, contrasenaNueva = Nueva });

        Assert.NotEqual(primero, segundo);
        Assert.Equal(HttpStatusCode.Gone, conElPrimero.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, conElSegundo.StatusCode);
    }

    [Fact]
    public async Task Un_enlace_vencido_o_inexistente_responde_410()
    {
        var (cliente, _, token) = await PedirAsync();
        await _fabrica.ConContextoAsync(contexto => contexto.SolicitudesRecuperacion
            .Where(solicitud => solicitud.UsadaEn == null && solicitud.VenceEn > DateTime.UtcNow.AddMinutes(59))
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(solicitud => solicitud.VenceEn, DateTime.UtcNow.AddSeconds(-1))));

        var vencido = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = Nueva });
        var inexistente = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token = "no-existe", contrasenaNueva = Nueva });

        Assert.Equal(HttpStatusCode.Gone, vencido.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, inexistente.StatusCode);
        Assert.Equal("enlace_no_valido", await ClienteDePrueba.CodigoAsync(inexistente));
    }

    [Theory]
    [InlineData("corta")]
    [InlineData("")]
    public async Task Una_contrasena_no_valida_responde_400_y_no_gasta_el_enlace(string contrasena)
    {
        var (cliente, _, token) = await PedirAsync();

        var invalida = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = contrasena });
        var valida = await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = Nueva });

        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(invalida));
        Assert.Contains("contrasenaNueva", await invalida.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NoContent, valida.StatusCode);
    }

    [Fact]
    public async Task La_contrasena_anterior_y_los_tokens_anteriores_dejan_de_servir()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var conSesion = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var cliente = _fabrica.CrearClienteDePrueba();
        await cliente.PostAsync("/api/cuenta/recuperacion", new { correo = usuario.Correo });
        var token = _fabrica.Correo.UltimoToken("recuperacion", usuario.Correo);

        await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = Nueva });

        Assert.Equal(HttpStatusCode.Unauthorized, (await conSesion.GetAsync("/api/sesion")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.IniciarSesionAsync(usuario.Correo)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.IniciarSesionAsync(usuario.Correo, Nueva)).StatusCode);
    }

    [Fact]
    public async Task Solo_se_guarda_el_hash_del_token()
    {
        var (_, _, token) = await PedirAsync();

        var guardados = await _fabrica.ConContextoAsync(contexto =>
            contexto.SolicitudesRecuperacion.Select(solicitud => solicitud.TokenHash).ToListAsync());

        Assert.DoesNotContain(token, guardados);
        Assert.All(guardados, hash => Assert.Equal(64, hash.Length));
    }

    private async Task<(ClienteDePrueba Cliente, string Correo, string Token)> PedirAsync()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = _fabrica.CrearClienteDePrueba();
        await cliente.PostAsync("/api/cuenta/recuperacion", new { correo = usuario.Correo });
        return (cliente, usuario.Correo, _fabrica.Correo.UltimoToken("recuperacion", usuario.Correo));
    }
}
