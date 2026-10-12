using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;

namespace LFClub.Pruebas.Integracion.Hermanos;

/// <summary>
/// Agregar un hermano con el documento de un jugador de otra cuenta en otro club se admite, queda
/// en espera y avisa a la familia si aprobarlo lo retirará de allí, sin cambiar nada en ese club; el
/// documento de otro rol de otra cuenta no se admite (historia 1, escenarios 11 y 12; RF-034, RF-035
/// y RF-039; research §12 de la 006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class DocumentoDeOtraCuentaPruebas
{
    private const string Responsable = "Marta Gómez";

    private readonly FabricaApi _fabrica;

    public DocumentoDeOtraCuentaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static Dictionary<string, object?> Datos(string numero) =>
        EscenarioHermanos.DatosDeHermano(numero, nombreResponsable: Responsable);

    private static async Task<HttpResponseMessage> AgregarAsync(EscenarioDocumentoCompartido e, string numero) =>
        await (await e.Hermanos.ClienteDeLaFamiliaAsync(e.Ana.Id)).PostAsync(e.Hermanos.Hermanos(e.Ana.Id), Datos(numero));

    // Escenario 1.11, RF-034 y RF-035.
    [Fact]
    public async Task Con_el_documento_de_un_jugador_activo_de_otra_cuenta_queda_en_espera_con_el_aviso_y_el_otro_no_cambia()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var (antes, fichaAntes, documentosAntes) = await e.LeerSinFiltroAsync(e.Caro.Id);

        // Escrito con mayúsculas y puntos: se guarda normalizado.
        var respuesta = await AgregarAsync(e, $" {e.Caro.NumeroDocumento.ToUpperInvariant()[..3]}.{e.Caro.NumeroDocumento[3..]} ");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = await respuesta.JsonAsync();
        Assert.True(cuerpo.GetProperty("retiraDeOtroClub").GetBoolean());
        Assert.Equal("EN_ESPERA", cuerpo.GetProperty("estadoIngreso").GetString());
        var hermano = (await e.Hermanos.JugadoresDeLaFamiliaAsync()).Single(jugador => jugador.Id == cuerpo.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(e.Caro.NumeroDocumento, hermano.NumeroDocumento);

        var texto = await respuesta.Content.ReadAsStringAsync();
        foreach (var ajeno in new[] { e.OtroClub.Id.ToString(), e.OtroClub.Nombre, e.CuentaDeCaro.Id.ToString(), e.CuentaDeCaro.Correo, e.Caro.Id.ToString() })
        {
            Assert.DoesNotContain(ajeno, texto);
        }

        var (caro, ficha, documentos) = await e.LeerSinFiltroAsync(e.Caro.Id);
        Assert.Equal((antes.Activo, antes.CategoriaId, antes.Equipos.Count), (caro.Activo, caro.CategoriaId, caro.Equipos.Count));
        Assert.Equal(fichaAntes!.UltimoCambioEn, ficha!.UltimoCambioEn);
        Assert.Equal(documentosAntes.Count, documentos.Count);

        // La sala de espera del club lo trae con el aviso.
        var enEspera = (await e.Presidente.ListaAsync($"/api/clubes/{e.Club.Id}/ingresos/en-espera"))
            .Single(ingreso => ingreso.GetProperty("usuarioRolId").GetGuid() == hermano.Id);
        Assert.True(enEspera.GetProperty("retiraDeOtroClub").GetBoolean());
    }

    [Fact]
    public async Task Con_el_de_un_jugador_retirado_o_en_espera_de_otra_cuenta_o_de_la_misma_cuenta_se_admite_sin_aviso()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        await _fabrica.Categorias.RetirarAsync(e.Dani, e.Dani);
        var esperando = await _fabrica.Sembrador.CrearIntegranteAsync(
            e.TercerClub, await _fabrica.Sembrador.CrearCuentaAsync(), Rol.JUGADOR, estadoIngreso: EstadoIngreso.EN_ESPERA);
        var deLaMismaCuenta = await _fabrica.Sembrador.CrearIntegranteAsync(e.TercerClub, e.Hermanos.Familia, Rol.JUGADOR);

        foreach (var numero in new[] { e.Dani.NumeroDocumento, esperando.NumeroDocumento, deLaMismaCuenta.NumeroDocumento })
        {
            var respuesta = await AgregarAsync(e, numero);

            Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
            Assert.False((await respuesta.JsonAsync()).GetProperty("retiraDeOtroClub").GetBoolean());
        }
    }

    // Escenario 1.12 y RF-039.
    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    public async Task Con_el_de_otro_rol_de_otra_cuenta_en_cualquier_club_responde_409_y_no_crea_nada(Rol rol)
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);
        var (_, otro) = await _fabrica.Sembrador.CrearIntegranteAsync(e.TercerClub, rol);

        var respuesta = await AgregarAsync(e, otro.NumeroDocumento);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("documento_en_otra_cuenta", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Single(await e.Hermanos.JugadoresDeLaFamiliaAsync());
    }

    // Supuesto 2: la segunda confirmación calcula el aviso en su momento.
    [Fact]
    public async Task Confirmar_dos_veces_devuelve_el_mismo_hermano_con_el_aviso_de_ese_momento()
    {
        var e = await EscenarioDocumentoCompartido.CrearAsync(_fabrica);

        var primera = await AgregarAsync(e, e.Caro.NumeroDocumento);
        await _fabrica.Categorias.RetirarAsync(e.Caro, e.Caro);
        var segunda = await AgregarAsync(e, e.Caro.NumeroDocumento);

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.True((await primera.JsonAsync()).GetProperty("retiraDeOtroClub").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        var cuerpo = await segunda.JsonAsync();
        Assert.False(cuerpo.GetProperty("retiraDeOtroClub").GetBoolean());
        Assert.Equal((await primera.JsonAsync()).GetProperty("usuarioRolId").GetGuid(), cuerpo.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(2, (await e.Hermanos.JugadoresDeLaFamiliaAsync()).Count);
    }
}
