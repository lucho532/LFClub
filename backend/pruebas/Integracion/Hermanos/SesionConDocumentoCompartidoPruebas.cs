using System.Net;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LFClub.Pruebas.Integracion.Hermanos;

/// <summary>
/// Con un documento en dos cuentas, entrar con él abre la del jugador activo; si no hay, la del que
/// espera; si tampoco, la del retirado; y la contraseña de la otra cuenta recibe la respuesta de
/// cualquier dato incorrecto (historia 2, escenarios 12 y 13; RF-040; research §15 de la 006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SesionConDocumentoCompartidoPruebas
{
    private const string ContrasenaDeCaro = "Contrasena-de-Caro-2";

    private readonly FabricaApi _fabrica;

    public SesionConDocumentoCompartidoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>El escenario con la cuenta de Caro con su propia contraseña, distinta de la de la familia de Ana.</summary>
    private async Task<EscenarioDocumentoCompartido> EscenarioAsync()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hash = _fabrica.Services.GetRequiredService<IHashContrasena>().Calcular(ContrasenaDeCaro);
        await _fabrica.ConContextoAsync(contexto => contexto.Usuarios
            .Where(cuenta => cuenta.Id == e.CuentaDeCaro.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(cuenta => cuenta.ContrasenaHash, hash)));
        return e;
    }

    private Task<Usuario> CuentaGuardadaAsync(Guid usuarioId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Usuarios.AsNoTracking().FirstAsync(cuenta => cuenta.Id == usuarioId));

    private async Task<string> CuerpoDeUnaContrasenaEquivocadaAsync(string identificador)
    {
        var respuesta = await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(identificador, "contrasena-equivocada");
        return await respuesta.Content.ReadAsStringAsync();
    }

    // Escenario 2.12: retirada en una cuenta y activo en otra.
    [Fact]
    public async Task Retirado_en_una_cuenta_y_activo_en_otra_solo_entra_la_contrasena_de_la_activa()
    {
        var e = await EscenarioAsync();
        await _fabrica.Categorias.RetirarAsync(e.Caro, e.Caro);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro, EstadoIngreso.APROBADO);
        var cliente = _fabrica.CrearClienteDePrueba();

        var conLaDeCaro = await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(e.Caro.NumeroDocumento, ContrasenaDeCaro);
        var conLaDeLaFamilia = await cliente.IniciarSesionAsync(e.Caro.NumeroDocumento);

        Assert.Equal(HttpStatusCode.Unauthorized, conLaDeCaro.StatusCode);
        Assert.Equal("credenciales_invalidas", await ClienteDePrueba.CodigoAsync(conLaDeCaro));
        Assert.Equal(
            await CuerpoDeUnaContrasenaEquivocadaAsync(e.Caro.NumeroDocumento), await conLaDeCaro.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, conLaDeLaFamilia.StatusCode);

        // Limitada al hermano y sin nada de la cuenta de Caro ni de su club.
        var sesion = await cliente.GetAsync("/api/sesion");
        var club = (await sesion.JsonAsync()).Lista("clubes").Single();
        Assert.Equal(hermano.Id, club.GetProperty("usuarioRolId").GetGuid());
        var cuerpo = await sesion.Content.ReadAsStringAsync();
        foreach (var ajeno in new[] { e.OtroClub.Id.ToString(), e.OtroClub.Nombre, e.Caro.Id.ToString(), e.CuentaDeCaro.Correo, "Caro" })
        {
            Assert.DoesNotContain(ajeno, cuerpo);
        }
    }

    // Escenario 2.13: activo en una y en espera en otra.
    [Fact]
    public async Task Activo_en_una_y_en_espera_en_otra_solo_entra_la_activa_y_la_familia_entra_con_su_correo()
    {
        var e = await EscenarioAsync();
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);

        var conLaDeCaro = await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(e.Caro.NumeroDocumento, ContrasenaDeCaro);
        var conLaDeLaFamilia = await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(e.Caro.NumeroDocumento);

        Assert.Equal(HttpStatusCode.OK, conLaDeCaro.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, conLaDeLaFamilia.StatusCode);
        Assert.Equal("credenciales_invalidas", await ClienteDePrueba.CodigoAsync(conLaDeLaFamilia));

        var familia = _fabrica.CrearClienteDePrueba();
        Assert.Equal(HttpStatusCode.OK, (await familia.IniciarSesionAsync(e.Hermanos.Familia.Correo)).StatusCode);
        var jugadores = (await (await familia.GetAsync("/api/sesion")).JsonAsync()).Lista("clubes")
            .Single(club => club.GetProperty("clubId").GetGuid() == e.Club.Id).Lista("jugadores");
        var enEspera = jugadores.Single(jugador => jugador.GetProperty("usuarioRolId").GetGuid() == hermano.Id);
        Assert.Equal("EN_ESPERA", enEspera.GetProperty("estadoIngreso").GetString());
    }

    // Supuesto 8: entre dos del mismo nivel, la cuenta del más reciente (el hermano se crea después).
    [Theory]
    [InlineData(EstadoIngreso.APROBADO, true)]
    [InlineData(EstadoIngreso.EN_ESPERA, true)]
    [InlineData(EstadoIngreso.APROBADO, false)]
    public async Task Entre_dos_del_mismo_nivel_abre_la_cuenta_del_mas_reciente(EstadoIngreso estado, bool activo)
    {
        var e = await EscenarioAsync();
        var numero = Sembrador.Unico("doc");
        var cuentaAntigua = await _fabrica.Sembrador.CrearCuentaAsync(contrasena: ContrasenaDeCaro);
        var antiguo = await _fabrica.Sembrador.CrearIntegranteAsync(
            e.TercerClub, cuentaAntigua, Rol.JUGADOR, numero, estadoIngreso: estado);
        if (!activo)
        {
            await _fabrica.Categorias.RetirarAsync(antiguo, antiguo);
        }

        await _fabrica.Hermanos.CrearHermanoAsync(e.Ana, estado, activo: activo, numeroDocumento: antiguo.NumeroDocumento);

        Assert.Equal(
            HttpStatusCode.OK, (await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(antiguo.NumeroDocumento)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(antiguo.NumeroDocumento, ContrasenaDeCaro)).StatusCode);
    }

    // Research §15: los fallos cuentan en la cuenta elegida, como cualquier otro.
    [Fact]
    public async Task Cinco_intentos_con_la_contrasena_de_la_otra_cuenta_bloquean_la_cuenta_elegida()
    {
        var e = await EscenarioAsync();
        await e.HermanoConDocumentoDeAsync(e.Caro);

        for (var intento = 0; intento < 5; intento++)
        {
            await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(e.Caro.NumeroDocumento);
        }

        Assert.True((await CuentaGuardadaAsync(e.CuentaDeCaro.Id)).Bloqueada);
        Assert.False((await CuentaGuardadaAsync(e.Hermanos.Familia.Id)).Bloqueada);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(e.Caro.NumeroDocumento, ContrasenaDeCaro)).StatusCode);
    }
}
