using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// La reincorporación se niega mientras el documento del jugador esté aprobado y activo con otra
/// cuenta, en cualquier club, sin revelar nada de ese club; y nunca quedan dos cuentas activas con
/// el mismo número, tampoco con operaciones a la vez (historia 3, escenarios 13 y 15; RF-041;
/// CE-013; research §19 de la 006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ReincorporarConDocumentoCompartidoPruebas
{
    private readonly FabricaApi _fabrica;

    public ReincorporarConDocumentoCompartidoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Reincorporacion(Club club, Guid id) => $"/api/clubes/{club.Id}/jugadores/{id}/reincorporacion";

    private static string Aprobacion(Club club, Guid id) => $"/api/clubes/{club.Id}/ingresos/{id}/aprobacion";

    private Task<List<UsuarioRol>> ActivosConAsync(string numero) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking()
            .Where(integrante => integrante.NumeroDocumento == numero && integrante.Activo
                && integrante.EstadoIngreso == EstadoIngreso.APROBADO)
            .ToListAsync());

    // Escenarios 3.13 y 3.15, RF-041 y CE-013.
    [Fact]
    public async Task Se_niega_mientras_el_hermano_esta_activo_y_se_admite_cuando_su_familia_lo_retira()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);
        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e.Club, hermano.Id))).StatusCode);

        var negada = await e.PresidenteDelOtroClub.PostAsync(Reincorporacion(e.OtroClub, e.Caro.Id));

        Assert.Equal(HttpStatusCode.Conflict, negada.StatusCode);
        Assert.Equal("documento_activo_en_otro_club", await ClienteDePrueba.CodigoAsync(negada));
        var cuerpo = await negada.Content.ReadAsStringAsync();
        foreach (var ajeno in new[] { e.Club.Id.ToString(), e.Club.Nombre, e.Hermanos.Familia.Id.ToString(), e.Hermanos.Familia.Correo, hermano.Id.ToString() })
        {
            Assert.DoesNotContain(ajeno, cuerpo);
        }

        Assert.False((await e.LeerSinFiltroAsync(e.Caro.Id)).Integrante.Activo);

        // La familia de Ana retira al hermano y entonces sí se reincorpora, sin el equipo que tenía.
        var familia = await e.Hermanos.ClienteDeLaFamiliaAsync(hermano.Id);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await familia.PostAsync($"/api/clubes/{e.Club.Id}/jugadores/{hermano.Id}/retiro")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await e.PresidenteDelOtroClub.PostAsync(Reincorporacion(e.OtroClub, e.Caro.Id))).StatusCode);
        var (caro, _, _) = await e.LeerSinFiltroAsync(e.Caro.Id);
        Assert.Equal((true, e.CategoriaDelOtroClub.Id), (caro.Activo, caro.CategoriaId));
        Assert.Empty(caro.Equipos);

        // Y ahora es el hermano el que no vuelve.
        var alReves = await e.Presidente.PostAsync(Reincorporacion(e.Club, hermano.Id));
        Assert.Equal(HttpStatusCode.Conflict, alReves.StatusCode);
        Assert.Equal("documento_activo_en_otro_club", await ClienteDePrueba.CodigoAsync(alReves));
    }

    [Fact]
    public async Task Con_el_numero_activo_solo_en_la_misma_cuenta_en_otro_club_se_reincorpora()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var numero = Sembrador.Unico("doc");
        var enElOtro = await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, e.Hermanos.Familia, Rol.JUGADOR, numero);
        await _fabrica.Categorias.RetirarAsync(enElOtro, enElOtro);
        await _fabrica.Sembrador.CrearIntegranteAsync(e.TercerClub, e.Hermanos.Familia, Rol.JUGADOR, numero);

        var respuesta = await e.PresidenteDelOtroClub.PostAsync(Reincorporacion(e.OtroClub, enElOtro.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // Supuesto 12: quien espera no impide; si después se aprueba, la baja retira al reincorporado.
    [Fact]
    public async Task Con_otra_cuenta_en_espera_se_reincorpora_y_si_despues_se_aprueba_lo_retira()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        await _fabrica.Categorias.RetirarAsync(e.Caro, e.Caro);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);

        Assert.Equal(HttpStatusCode.OK, (await e.PresidenteDelOtroClub.PostAsync(Reincorporacion(e.OtroClub, e.Caro.Id))).StatusCode);
        Assert.True((await e.LeerSinFiltroAsync(e.Caro.Id)).Integrante.Activo);

        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e.Club, hermano.Id))).StatusCode);
        Assert.False((await e.LeerSinFiltroAsync(e.Caro.Id)).Integrante.Activo);
        Assert.Single(await ActivosConAsync(e.Caro.NumeroDocumento));
    }

    [Fact]
    public async Task Dos_reincorporaciones_a_la_vez_con_el_mismo_numero_dejan_una_sola_activa()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        await _fabrica.Categorias.RetirarAsync(e.Caro, e.Caro);
        var hermano = await _fabrica.Hermanos.CrearHermanoAsync(
            e.Ana, EstadoIngreso.APROBADO, activo: false, numeroDocumento: e.Caro.NumeroDocumento);

        var respuestas = await Task.WhenAll(
            e.PresidenteDelOtroClub.PostAsync(Reincorporacion(e.OtroClub, e.Caro.Id)),
            e.Presidente.PostAsync(Reincorporacion(e.Club, hermano.Id)));

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Conflict], respuestas.Select(respuesta => respuesta.StatusCode).Order());
        Assert.Single(await ActivosConAsync(e.Caro.NumeroDocumento));
    }

    [Fact]
    public async Task Una_reincorporacion_y_una_aprobacion_con_baja_a_la_vez_nunca_dejan_dos_activas()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        await _fabrica.Categorias.RetirarAsync(e.Caro, e.Caro);
        var hermano = await e.HermanoConDocumentoDeAsync(e.Caro);

        var respuestas = await Task.WhenAll(
            e.PresidenteDelOtroClub.PostAsync(Reincorporacion(e.OtroClub, e.Caro.Id)),
            e.Presidente.PostAsync(Aprobacion(e.Club, hermano.Id)));

        Assert.All(respuestas, respuesta => Assert.True(
            respuesta.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict, $"{(int)respuesta.StatusCode}"));
        Assert.Equal(HttpStatusCode.OK, respuestas[1].StatusCode);
        Assert.Single(await ActivosConAsync(e.Caro.NumeroDocumento));
    }
}
