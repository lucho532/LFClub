using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// El PRESIDENTE cambia cualquier dato de la ficha de un jugador de su club, igual que si lo
/// hubiera hecho la familia, y nada de la de otro club (historia 4, escenario 8; historia 1,
/// escenario 9; RF-018; CE-001, CE-006 y CE-012).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PresidenteCambiaFichaPruebas
{
    private const DocumentoPedido Certificado = DocumentoPedido.CERTIFICADO_SALUD;

    private readonly FabricaApi _fabrica;

    public PresidenteCambiaFichaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    // CE-001: lo que guarda el presidente es exactamente lo que lee la familia.
    [Fact]
    public async Task Guarda_contacto_emergencia_seguridad_social_y_datos_clinicos_y_la_familia_lee_lo_mismo()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.Presidente.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var delPresidente = await respuesta.JsonAsync();
        var deLaFamilia = await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id);
        foreach (var grupo in new[] { "contacto", "contactoEmergencia", "seguridadSocial", "datosClinicos", "ultimoCambio" })
        {
            Assert.Equal(delPresidente.GetProperty(grupo).GetRawText(), deLaFamilia.GetProperty(grupo).GetRawText());
        }

        Assert.Equal(EscenarioFicha.CelularNuevo, deLaFamilia.GetProperty("contacto").GetProperty("celular").GetString());
        Assert.Equal("Polen", deLaFamilia.GetProperty("datosClinicos").GetProperty("alergias").GetString());
    }

    // Historia 1, escenario 9 y CE-012.
    [Fact]
    public async Task El_ultimo_cambio_trae_el_nombre_del_presidente_y_la_familia_lo_ve()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo(celular: "3001110000"));

        await e.Presidente.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());

        var cambio = (await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id)).GetProperty("ultimoCambio");
        var presidente = e.Base.IntegrantePresidente;
        Assert.Equal($"{presidente.Nombres} {presidente.Apellidos}", cambio.GetProperty("autor").GetString());
        Assert.False(cambio.GetProperty("porLaCuentaDelJugador").GetBoolean());
    }

    [Fact]
    public async Task Sube_y_reemplaza_un_documento_que_la_familia_abre()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Certificado), SembradorFichas.Jpeg());
        var nuevo = SembradorFichas.Pdf(128);

        var respuesta = await e.Presidente.SubirAsync(e.Documento(e.Ana.Id, Certificado), nuevo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(nuevo, await (await e.CuentaDeAna.GetAsync(e.Documento(e.Ana.Id, Certificado))).Content.ReadAsByteArrayAsync());
        Assert.False((await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id))
            .GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());
    }

    // También sobre un jugador sin categoría y sobre uno retirado.
    [Fact]
    public async Task Lo_hace_tambien_sobre_un_jugador_sin_categoria_y_sobre_uno_retirado()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        foreach (var jugador in new[] { e.Caro, e.Dani })
        {
            var formulario = await e.Presidente.PutAsync(e.Ficha(jugador.Id), EscenarioFicha.Cuerpo());
            var archivo = await e.Presidente.SubirAsync(e.Documento(jugador.Id, Certificado), SembradorFichas.Png());
            var documento = await e.Presidente.PutAsync(
                e.DocumentoIdentidad(jugador.Id), new { tipoDocumento = "REGISTRO_CIVIL", numeroDocumento = Sembrador.Unico("rc") });

            Assert.All(new[] { formulario, archivo, documento }, respuesta => Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode));
            Assert.Equal("Nueva EPS", (await e.FichaGuardadaAsync(jugador.Id))!.EntidadSalud);
            Assert.Single(await e.DocumentosGuardadosAsync(jugador.Id));
            Assert.Equal(TipoDocumento.REGISTRO_CIVIL, (await e.JugadorGuardadoAsync(jugador.Id)).TipoDocumento);
        }
    }

    // RF-018: el correo no se cambia desde la ficha.
    [Fact]
    public async Task No_cambia_el_correo_un_correo_en_el_cuerpo_se_ignora()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var correo = (await e.JugadorGuardadoAsync(e.Ana.Id)).Usuario!.Correo;
        var cuerpo = EscenarioFicha.Cuerpo();
        cuerpo["correo"] = "otro@lfclub.test";

        var respuesta = await e.Presidente.PutAsync(e.Ficha(e.Ana.Id), cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(correo, (await respuesta.JsonAsync()).GetProperty("contacto").GetProperty("correo").GetString());
        Assert.Equal(correo, (await e.JugadorGuardadoAsync(e.Ana.Id)).Usuario!.Correo);
    }

    // CE-006: en las cuatro operaciones de cambio, con el identificador en su ruta y en la ajena.
    [Fact]
    public async Task El_presidente_de_otro_club_recibe_404_en_las_cuatro_operaciones()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var ajeno = e.Base.PresidenteDeOtroClub;
        var antes = await e.JugadorGuardadoAsync(e.Ana.Id);

        foreach (var ficha in new[] { e.Ficha(e.Ana.Id), EscenarioFicha.Ficha(e.Base.OtroClub.Id, e.Ana.Id) })
        {
            var respuestas = new[]
            {
                await ajeno.PutAsync(ficha, EscenarioFicha.Cuerpo()),
                await ajeno.PutAsync($"{ficha}/documento-identidad", new { tipoDocumento = "REGISTRO_CIVIL", numeroDocumento = Sembrador.Unico("rc") }),
                await ajeno.PutAsync($"{ficha}/identidad", new { nombres = "Otro", apellidos = "Distinto", fechaNacimiento = "2010-01-01" }),
                await ajeno.SubirAsync($"{ficha}/documentos/{Certificado}", SembradorFichas.Pdf()),
            };

            foreach (var respuesta in respuestas)
            {
                Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
                Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            }
        }

        var despues = await e.JugadorGuardadoAsync(e.Ana.Id);
        Assert.Equal((antes.Nombres, antes.NumeroDocumento, antes.FechaNacimiento), (despues.Nombres, despues.NumeroDocumento, despues.FechaNacimiento));
        Assert.Equal(antes.Usuario!.Celular, despues.Usuario!.Celular);
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
        Assert.Empty(await e.DocumentosGuardadosAsync(e.Ana.Id));
    }
}
