using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Hermanos;

/// <summary>
/// Aprobar al hermano que tiene el documento de un jugador activo de otra cuenta lo retira de su
/// club en la misma operación, sin tocar nada más de él ni avisar a nadie; rechazarlo no cambia el
/// otro club (historia 3, escenarios 10 a 12; RF-035 a RF-037; CE-011; research §13 de la 006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class BajaEnOtroClubPruebas
{
    private readonly FabricaApi _fabrica;

    public BajaEnOtroClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Aprobacion(Club club, Guid id) => $"/api/clubes/{club.Id}/ingresos/{id}/aprobacion";

    private static string Rechazo(Club club, Guid id) => $"/api/clubes/{club.Id}/ingresos/{id}/rechazo";

    private Task<Usuario> CuentaGuardadaAsync(Guid id) => _fabrica.ConContextoAsync(contexto =>
        contexto.Usuarios.AsNoTracking().FirstAsync(cuenta => cuenta.Id == id));

    // Escenario 3.10, RF-036, RF-037 y CE-011.
    [Fact]
    public async Task Aprobar_retira_en_la_misma_operacion_al_jugador_de_la_otra_cuenta_y_no_toca_nada_mas()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);
        var (antes, fichaAntes, documentosAntes) = await e.LeerSinFiltroAsync(e.Caro.Id);
        var correosAntes = _fabrica.Correo.Enviados.Count;

        var respuesta = await e.Presidente.PostAsync(Aprobacion(e.Club, hermano.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var (aprobado, _, _) = await e.LeerSinFiltroAsync(hermano.Id);
        Assert.Equal((EstadoIngreso.APROBADO, true, e.Hermanos.CategoriaDeAna.Id), (aprobado.EstadoIngreso, aprobado.Activo, aprobado.CategoriaId));

        var (caro, ficha, documentos) = await e.LeerSinFiltroAsync(e.Caro.Id);
        Assert.False(caro.Activo);
        Assert.Null(caro.CategoriaId);
        Assert.Empty(caro.Equipos);
        Assert.Equal(aprobado.AprobadoEn, caro.RetiradoEn);
        Assert.Null(caro.RetiradoPorUsuarioId);
        Assert.Null(caro.RetiradoPorNombre);
        Assert.Equal((antes.EstadoIngreso, antes.Nombres, antes.NumeroDocumento), (caro.EstadoIngreso, caro.Nombres, caro.NumeroDocumento));
        Assert.Equal((fichaAntes!.EmergenciaNombre, fichaAntes.UltimoCambioEn), (ficha!.EmergenciaNombre, ficha.UltimoCambioEn));
        Assert.Equal(documentosAntes.Count, documentos.Count);
        Assert.Equal(e.CuentaDeCaro.ContrasenaHash, (await CuentaGuardadaAsync(e.CuentaDeCaro.Id)).ContrasenaHash);
        Assert.Equal(correosAntes, _fabrica.Correo.Enviados.Count);

        // El otro club la ve entre sus retirados, sin autor.
        var retirada = (await e.PresidenteDelOtroClub.ListaAsync($"/api/clubes/{e.OtroClub.Id}/jugadores/retirados"))
            .Single(jugador => jugador.GetProperty("usuarioRolId").GetGuid() == e.Caro.Id);
        Assert.Equal(string.Empty, retirada.GetProperty("retiradoPor").GetString());
    }

    // Escenario 3.12: la familia de Caro entra, pero ya no al otro club.
    [Fact]
    public async Task La_familia_del_retirado_sigue_entrando_y_ve_que_ya_no_esta_en_ese_club()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);
        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e.Club, hermano.Id))).StatusCode);

        var familia = _fabrica.CrearClienteDePrueba();
        Assert.Equal(HttpStatusCode.OK, (await familia.IniciarSesionAsync(e.CuentaDeCaro.Correo)).StatusCode);
        var alClub = await familia.GetAsync($"/api/clubes/{e.OtroClub.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, alClub.StatusCode);
        Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(alClub));
    }

    [Fact]
    public async Task Con_el_numero_activo_en_dos_clubes_de_otras_cuentas_los_retira_en_los_dos()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var enElTercero = await _fabrica.Sembrador.CrearIntegranteAsync(
            e.TercerClub, await _fabrica.Sembrador.CrearCuentaAsync(), Rol.JUGADOR, e.Caro.NumeroDocumento);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);

        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e.Club, hermano.Id))).StatusCode);

        Assert.False((await e.LeerSinFiltroAsync(e.Caro.Id)).Integrante.Activo);
        Assert.False((await e.LeerSinFiltroAsync(enElTercero.Id)).Integrante.Activo);
    }

    // El retiro de antes se conserva tal cual: la baja se decide con lo que hay al aprobar.
    [Fact]
    public async Task Si_ya_estaba_retirado_alli_nada_cambia()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);
        await _fabrica.Categorias.RetirarAsync(e.Caro, e.Caro);
        var (antes, _, _) = await e.LeerSinFiltroAsync(e.Caro.Id);

        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e.Club, hermano.Id))).StatusCode);

        var (caro, _, _) = await e.LeerSinFiltroAsync(e.Caro.Id);
        Assert.Equal((antes.RetiradoEn, antes.RetiradoPorNombre), (caro.RetiradoEn, caro.RetiradoPorNombre));
    }

    // Es el mismo niño inscrito en los dos clubes, y quien espera en otro club no se toca.
    [Fact]
    public async Task No_retira_a_la_misma_cuenta_en_otro_club_ni_a_quien_espera_en_otro_club()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var mismoNinio = Sembrador.Unico("doc");
        var enElOtroClub = await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, e.Hermanos.Familia, Rol.JUGADOR, mismoNinio);
        var deLaMismaCuenta = await _fabrica.Hermanos.CrearHermanoAsync(e.Ana, numeroDocumento: enElOtroClub.NumeroDocumento);
        var esperando = await _fabrica.Sembrador.CrearIntegranteAsync(
            e.TercerClub, await _fabrica.Sembrador.CrearCuentaAsync(), Rol.JUGADOR, estadoIngreso: EstadoIngreso.EN_ESPERA);
        var conElDeQuienEspera = await e.HermanoConDocumentoDeAsync(esperando);

        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e.Club, deLaMismaCuenta.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e.Club, conElDeQuienEspera.Id))).StatusCode);

        Assert.True((await e.LeerSinFiltroAsync(enElOtroClub.Id)).Integrante.Activo);
        var (otro, _, _) = await e.LeerSinFiltroAsync(esperando.Id);
        Assert.Equal((EstadoIngreso.EN_ESPERA, true), (otro.EstadoIngreso, otro.Activo));
    }

    // Escenario 3.11 y RF-035.
    [Fact]
    public async Task Rechazar_al_hermano_con_el_documento_de_otro_lo_borra_y_el_otro_no_cambia()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Dani);

        Assert.Equal(HttpStatusCode.NoContent, (await e.Presidente.PostAsync(Rechazo(e.Club, hermano.Id))).StatusCode);

        Assert.DoesNotContain(await e.Hermanos.JugadoresDeLaFamiliaAsync(), jugador => jugador.Id == hermano.Id);
        var (dani, _, _) = await e.LeerSinFiltroAsync(e.Dani.Id);
        Assert.Equal((true, e.CategoriaDelOtroClub.Id), (dani.Activo, dani.CategoriaId));
    }

    // Supuesto 7: al aprobar se vuelve a aplicar RF-039.
    [Fact]
    public async Task Si_el_numero_paso_a_ser_de_un_directivo_de_otra_cuenta_no_se_aprueba_y_nada_cambia()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hermano = await _fabrica.Hermanos.CrearHermanoAsync(e.Ana);
        await _fabrica.Sembrador.CrearIntegranteAsync(
            e.TercerClub, await _fabrica.Sembrador.CrearCuentaAsync(), Rol.DIRECTIVO, hermano.NumeroDocumento);

        var respuesta = await e.Presidente.PostAsync(Aprobacion(e.Club, hermano.Id));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("documento_en_otra_cuenta", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(EstadoIngreso.EN_ESPERA, (await e.LeerSinFiltroAsync(hermano.Id)).Integrante.EstadoIngreso);
    }

    // Research §13: los clubes se bloquean en orden y en una sola sentencia.
    [Fact]
    public async Task Dos_aprobaciones_cruzadas_a_la_vez_terminan_las_dos()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var enA = await e.HermanoConDocumentoDeAsync(e.Caro);
        var enB = await _fabrica.Hermanos.CrearHermanoAsync(e.Dani, numeroDocumento: e.Hermanos.Beto.NumeroDocumento);

        var respuestas = await Task.WhenAll(
            e.Presidente.PostAsync(Aprobacion(e.Club, enA.Id)),
            e.PresidenteDelOtroClub.PostAsync(Aprobacion(e.OtroClub, enB.Id)));

        Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode));
        Assert.False((await e.LeerSinFiltroAsync(e.Caro.Id)).Integrante.Activo);
        Assert.False((await e.LeerSinFiltroAsync(e.Hermanos.Beto.Id)).Integrante.Activo);
    }
}
