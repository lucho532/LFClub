using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// Un DIRECTIVO y un ENTRENADOR no cambian ningún dato de ninguna ficha, tampoco el entrenador de
/// la categoría del jugador (historia 2, escenario 7; RF-019; CE-005). Reciben
/// <c>403 rol_no_autorizado</c> antes de que se mire el identificador.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SoloLecturaPruebas
{
    private readonly FabricaApi _fabrica;

    public SoloLecturaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task No_guardan_el_formulario_de_la_ficha()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        foreach (var (rol, cliente) in e.QuienesSoloConsultan)
        {
            // Ana está en la categoría que entrena el entrenador; Beto, no.
            foreach (var jugador in new[] { e.Ana, e.Beto })
            {
                var respuesta = await cliente.PutAsync(e.Ficha(jugador.Id), EscenarioFicha.Cuerpo());

                await NegadoAsync(respuesta, rol);
                Assert.Null(await e.FichaGuardadaAsync(jugador.Id));
                Assert.Equal("3001234567", (await e.JugadorGuardadoAsync(jugador.Id)).Usuario!.Celular);
            }
        }
    }

    [Fact]
    public async Task No_suben_ni_reemplazan_documentos()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var anterior = SembradorFichas.Pdf(64);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, DocumentoPedido.CERTIFICADO_SALUD, anterior, "application/pdf");

        foreach (var (rol, cliente) in e.QuienesSoloConsultan)
        {
            foreach (var documento in Enum.GetValues<DocumentoPedido>())
            {
                await NegadoAsync(await cliente.SubirAsync(e.Documento(e.Ana.Id, documento), SembradorFichas.Png()), rol);
            }
        }

        Assert.Equal(anterior, Assert.Single(await e.DocumentosGuardadosAsync(e.Ana.Id)).Contenido);
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    [Fact]
    public async Task No_cambian_el_documento_de_identidad_ni_corrigen_la_identidad()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        foreach (var (rol, cliente) in e.QuienesSoloConsultan)
        {
            foreach (var jugador in new[] { e.Ana, e.Beto })
            {
                await NegadoAsync(
                    await cliente.PutAsync(
                        e.DocumentoIdentidad(jugador.Id),
                        new { tipoDocumento = "REGISTRO_CIVIL", numeroDocumento = Sembrador.Unico("rc") }),
                    rol);
                await NegadoAsync(
                    await cliente.PutAsync(
                        e.Identidad(jugador.Id), new { nombres = "Otro", apellidos = "Distinto", fechaNacimiento = "2010-01-01" }),
                    rol);

                var guardado = await e.JugadorGuardadoAsync(jugador.Id);
                Assert.Equal(
                    (jugador.Nombres, jugador.Apellidos, jugador.FechaNacimiento, jugador.TipoDocumento, jugador.NumeroDocumento),
                    (guardado.Nombres, guardado.Apellidos, guardado.FechaNacimiento, guardado.TipoDocumento, guardado.NumeroDocumento));
                Assert.Null(await e.FichaGuardadaAsync(jugador.Id));
            }
        }
    }

    // Las cuatro operaciones de cambio responden igual para un jugador que no existe: no revelan nada.
    [Fact]
    public async Task Reciben_el_mismo_403_para_un_identificador_que_no_existe()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var nadie = Guid.NewGuid();

        foreach (var (rol, cliente) in e.QuienesSoloConsultan)
        {
            await NegadoAsync(await cliente.PutAsync(e.Ficha(nadie), EscenarioFicha.Cuerpo()), rol);
            await NegadoAsync(await cliente.PutAsync(e.DocumentoIdentidad(nadie), new { }), rol);
            await NegadoAsync(await cliente.PutAsync(e.Identidad(nadie), new { }), rol);
            await NegadoAsync(
                await cliente.SubirAsync(e.Documento(nadie, DocumentoPedido.CERTIFICADO_SALUD), SembradorFichas.Pdf()), rol);
        }
    }

    private static async Task NegadoAsync(HttpResponseMessage respuesta, string rol)
    {
        Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{rol}: {(int)respuesta.StatusCode}");
        Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
    }
}
