using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>Bloqueo de la cuenta al quinto fallo seguido, hasta recuperar la contraseña (RF-005).</summary>
[Collection(ColeccionApi.Nombre)]
public class BloqueoPruebas
{
    private readonly FabricaApi _fabrica;

    public BloqueoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Cinco_fallos_bloquean_y_la_contrasena_correcta_deja_de_servir()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = _fabrica.CrearClienteDePrueba();

        for (var intento = 1; intento <= 4; intento++)
        {
            await cliente.IniciarSesionAsync(usuario.Correo, "incorrecta");
            Assert.False((await CuentaAsync(usuario.Id)).Bloqueada);
        }

        await cliente.IniciarSesionAsync(usuario.Correo, "incorrecta");
        var conLaCorrecta = await cliente.IniciarSesionAsync(usuario.Correo);

        var cuenta = await CuentaAsync(usuario.Id);
        Assert.True(cuenta.Bloqueada);
        Assert.Equal(5, cuenta.IntentosFallidos);
        Assert.Equal(HttpStatusCode.Unauthorized, conLaCorrecta.StatusCode);
        Assert.Equal("credenciales_invalidas", await ClienteDePrueba.CodigoAsync(conLaCorrecta));
    }

    [Fact]
    public async Task Un_acierto_al_cuarto_fallo_reinicia_la_cuenta()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = _fabrica.CrearClienteDePrueba();

        for (var intento = 1; intento <= 4; intento++)
        {
            await cliente.IniciarSesionAsync(usuario.Correo, "incorrecta");
        }

        var acierto = await cliente.IniciarSesionAsync(usuario.Correo);
        Assert.Equal(HttpStatusCode.OK, acierto.StatusCode);
        Assert.Equal(0, (await CuentaAsync(usuario.Id)).IntentosFallidos);

        // Vuelven a hacer falta cinco fallos seguidos.
        for (var intento = 1; intento <= 4; intento++)
        {
            await cliente.IniciarSesionAsync(usuario.Correo, "incorrecta");
        }

        Assert.Equal(HttpStatusCode.OK, (await cliente.IniciarSesionAsync(usuario.Correo)).StatusCode);
    }

    [Fact]
    public async Task La_recuperacion_por_correo_desbloquea_la_cuenta()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = _fabrica.CrearClienteDePrueba();
        for (var intento = 1; intento <= 5; intento++)
        {
            await cliente.IniciarSesionAsync(usuario.Correo, "incorrecta");
        }

        await cliente.PostAsync("/api/cuenta/recuperacion", new { correo = usuario.Correo });
        var token = _fabrica.Correo.UltimoToken("recuperacion", usuario.Correo);
        await cliente.PostAsync("/api/cuenta/recuperacion/confirmacion", new { token, contrasenaNueva = "contrasena-recuperada" });

        var cuenta = await CuentaAsync(usuario.Id);
        Assert.False(cuenta.Bloqueada);
        Assert.Equal(0, cuenta.IntentosFallidos);
        Assert.Equal(HttpStatusCode.OK, (await cliente.IniciarSesionAsync(usuario.Correo, "contrasena-recuperada")).StatusCode);
    }

    [Fact]
    public async Task Cinco_fallos_simultaneos_no_pierden_ninguno()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();

        await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => _fabrica.CrearClienteDePrueba().IniciarSesionAsync(usuario.Correo, "incorrecta")));

        var cuenta = await CuentaAsync(usuario.Id);
        Assert.Equal(5, cuenta.IntentosFallidos);
        Assert.True(cuenta.Bloqueada);
    }

    [Fact]
    public async Task La_respuesta_no_distingue_una_cuenta_bloqueada_de_una_inexistente()
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var cliente = _fabrica.CrearClienteDePrueba();
        for (var intento = 1; intento <= 5; intento++)
        {
            await cliente.IniciarSesionAsync(usuario.Correo, "incorrecta");
        }

        var bloqueada = await cliente.IniciarSesionAsync(usuario.Correo);
        var inexistente = await cliente.IniciarSesionAsync(Sembrador.CorreoUnico());

        Assert.Equal(inexistente.StatusCode, bloqueada.StatusCode);
        Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await bloqueada.Content.ReadAsStringAsync());
        Assert.Contains("Olvidé mi contraseña", await ClienteDePrueba.TituloAsync(bloqueada));
    }

    private Task<Usuario> CuentaAsync(Guid usuarioId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Usuarios.AsNoTracking().SingleAsync(cuenta => cuenta.Id == usuarioId));
}
