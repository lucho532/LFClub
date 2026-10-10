using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// La sala de espera avisa al PRESIDENTE cuando aprobar un ingreso retirará a un jugador de otra
/// cuenta en otro club, con un booleano que se calcula al abrirla y que no dice nada más de ese
/// club ni de esa cuenta (historia 3, escenario 9; RF-038; research §14 de la 006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisoEnSalaDeEsperaPruebas
{
    private readonly FabricaApi _fabrica;

    public AvisoEnSalaDeEsperaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string EnEspera(Club club) => $"/api/clubes/{club.Id}/ingresos/en-espera";

    private static async Task<bool> AvisoDeAsync(EscenarioDocumentoCompartido e, Guid usuarioRolId) =>
        (await e.Presidente.ListaAsync(EnEspera(e.Club)))
            .Single(ingreso => ingreso.GetProperty("usuarioRolId").GetGuid() == usuarioRolId)
            .GetProperty("retiraDeOtroClub").GetBoolean();

    // Escenario 3.9 y RF-038.
    [Fact]
    public async Task Con_el_documento_de_un_jugador_activo_de_otra_cuenta_avisa_sin_decir_nada_del_otro_club()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);

        var respuesta = await e.Presidente.GetAsync(EnEspera(e.Club));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(await AvisoDeAsync(e, hermano.Id));
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        foreach (var ajeno in new[]
        {
            e.OtroClub.Id.ToString(), e.OtroClub.Nombre, e.CuentaDeCaro.Id.ToString(), e.CuentaDeCaro.Correo,
            e.Caro.Id.ToString(), "Caro", "Ruiz",
        })
        {
            Assert.DoesNotContain(ajeno, cuerpo);
        }
    }

    [Fact]
    public async Task Sin_jugador_activo_de_otra_cuenta_no_avisa()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var retirado = await _fabrica.Sembrador.CrearIntegranteAsync(
            e.TercerClub, await _fabrica.Sembrador.CrearCuentaAsync(), Rol.JUGADOR);
        await _fabrica.Categorias.RetirarAsync(retirado, retirado);
        var esperando = await _fabrica.Sembrador.CrearIntegranteAsync(
            e.TercerClub, await _fabrica.Sembrador.CrearCuentaAsync(), Rol.JUGADOR, estadoIngreso: EstadoIngreso.EN_ESPERA);
        var deLaMismaCuenta = await _fabrica.Sembrador.CrearIntegranteAsync(e.TercerClub, e.Hermanos.Familia, Rol.JUGADOR);

        UsuarioRol[] hermanos =
        [
            await e.HermanoConDocumentoDeAsync(retirado),
            await e.HermanoConDocumentoDeAsync(esperando),
            await e.HermanoConDocumentoDeAsync(deLaMismaCuenta),
            await _fabrica.Hermanos.CrearHermanoAsync(e.Ana),
        ];

        foreach (var hermano in hermanos)
        {
            Assert.False(await AvisoDeAsync(e, hermano.Id));
        }
    }

    // Caso límite: se calcula al abrir, con lo que haya en ese momento.
    [Fact]
    public async Task Si_el_otro_club_lo_retira_deja_de_avisar_y_si_lo_reincorpora_vuelve_a_avisar()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);

        await _fabrica.Categorias.RetirarAsync(e.Caro, e.Caro);
        Assert.False(await AvisoDeAsync(e, hermano.Id));

        await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(integrante => integrante.Id == e.Caro.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(integrante => integrante.Activo, true)));
        Assert.True(await AvisoDeAsync(e, hermano.Id));
    }

    [Fact]
    public async Task Con_varios_ingresos_cada_uno_lleva_su_propio_aviso()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var conCaro = await e.HermanoConDocumentoDeAsync(e.Caro);
        var sinNadie = await _fabrica.Hermanos.CrearHermanoAsync(e.Ana);

        var lista = await e.Presidente.ListaAsync(EnEspera(e.Club));

        bool Aviso(Guid id) => lista.Single(ingreso => ingreso.GetProperty("usuarioRolId").GetGuid() == id)
            .GetProperty("retiraDeOtroClub").GetBoolean();
        Assert.True(Aviso(conCaro.Id));
        Assert.False(Aviso(sinNadie.Id));
    }
}
