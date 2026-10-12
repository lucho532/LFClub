using System.Net;
using System.Text.Json;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// La familia entrega la documentación del jugador: sube, abre y reemplaza un archivo por cada
/// documento pedido (historia 3, escenarios 1 a 4; RF-027 a RF-030, RF-038; CE-010).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class DocumentosPruebas
{
    private const DocumentoPedido Copia = DocumentoPedido.COPIA_DOCUMENTO_IDENTIDAD;
    private const DocumentoPedido Certificado = DocumentoPedido.CERTIFICADO_SALUD;

    private readonly FabricaApi _fabrica;

    public DocumentosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public static TheoryData<string, string, string> Formatos => new()
    {
        { "pdf", "application/pdf", "copia-documento-identidad.pdf" },
        { "jpeg", "image/jpeg", "copia-documento-identidad.jpg" },
        { "png", "image/png", "copia-documento-identidad.png" },
        { "webp", "image/webp", "copia-documento-identidad.webp" },
    };

    private static byte[] Archivo(string formato) => formato switch
    {
        "pdf" => SembradorFichas.Pdf(),
        "jpeg" => SembradorFichas.Jpeg(),
        "png" => SembradorFichas.Png(),
        _ => SembradorFichas.Webp(),
    };

    // Escenario 3.2: entregado, con su fecha, y la familia lo abre.
    [Theory]
    [MemberData(nameof(Formatos))]
    public async Task Subir_un_archivo_valido_lo_deja_entregado_y_abrirlo_devuelve_los_mismos_bytes(
        string formato, string tipo, string nombre)
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var contenido = Archivo(formato);
        var antes = DateTime.UtcNow.AddSeconds(-1);

        // El nombre y el tipo que declara quien sube no cuentan: decide el contenido.
        var subida = await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Copia), contenido, "cualquier-nombre.txt");

        Assert.Equal(HttpStatusCode.OK, subida.StatusCode);
        Assert.True(subida.SinCache());
        var ficha = await subida.JsonAsync();
        var entregado = ficha.DocumentoDe(Copia);
        Assert.True(entregado.GetProperty("entregado").GetBoolean());
        Assert.InRange(entregado.GetProperty("subidoEn").GetDateTime(), antes, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(tipo, entregado.GetProperty("tipoContenido").GetString());
        Assert.Equal(contenido.Length, entregado.GetProperty("tamanoBytes").GetInt32());

        var abierto = await e.CuentaDeAna.GetAsync(e.Documento(e.Ana.Id, Copia));
        Assert.Equal(HttpStatusCode.OK, abierto.StatusCode);
        Assert.Equal(contenido, await abierto.Content.ReadAsByteArrayAsync());
        Assert.Equal(tipo, abierto.Tipo());
        Assert.True(abierto.SinCache());
        Assert.Equal("nosniff", abierto.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal($"inline; filename=\"{nombre}\"", abierto.Disposicion());
    }

    // Escenario 3.3 y RF-029: un solo archivo por documento; el anterior deja de existir.
    [Fact]
    public async Task Subir_otro_archivo_al_mismo_documento_lo_reemplaza_y_queda_una_sola_fila()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Certificado), SembradorFichas.Jpeg(), "foto.jpg");
        var nuevo = SembradorFichas.Pdf(2048);

        var subida = await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Certificado), nuevo);

        Assert.Equal(HttpStatusCode.OK, subida.StatusCode);
        var abierto = await e.CuentaDeAna.GetAsync(e.Documento(e.Ana.Id, Certificado));
        Assert.Equal(nuevo, await abierto.Content.ReadAsByteArrayAsync());
        Assert.Equal("application/pdf", abierto.Tipo());
        Assert.Equal("inline; filename=\"certificado-salud.pdf\"", abierto.Disposicion());
        var guardado = Assert.Single(await e.DocumentosGuardadosAsync(e.Ana.Id));
        Assert.Equal(nuevo, guardado.Contenido);
        Assert.Equal(2048, guardado.TamanoBytes);
    }

    [Fact]
    public async Task Subir_a_un_documento_no_cambia_el_otro_y_sella_el_ultimo_cambio()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        Assert.False((await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id)).Tiene("ultimoCambio"));

        var ficha = await (await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Copia), SembradorFichas.Png())).JsonAsync();

        Assert.True(ficha.DocumentoDe(Copia).GetProperty("entregado").GetBoolean());
        Assert.False(ficha.DocumentoDe(Certificado).GetProperty("entregado").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await e.CuentaDeAna.GetAsync(e.Documento(e.Ana.Id, Certificado))).StatusCode);
        Assert.True(ficha.GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());

        // El sello no toca los datos de salud, que siguen vacíos.
        var guardada = (await e.FichaGuardadaAsync(e.Ana.Id))!;
        Assert.Null(guardada.Alergias);
        Assert.Equal(e.Ana.UsuarioId, guardada.UltimoCambioPorUsuarioId);
    }

    // Escenario 3.4 y CE-010: se rechaza con su motivo y se conserva lo que había.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Un_archivo_rechazado_conserva_el_anterior_o_deja_el_documento_pendiente(bool habiaUno)
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var ruta = e.Documento(e.Ana.Id, Copia);
        var anterior = SembradorFichas.Pdf();
        if (habiaUno)
        {
            await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, anterior, "application/pdf");
        }

        var rechazos = new (string Caso, HttpResponseMessage Respuesta, string Codigo)[]
        {
            ("disfrazado", await e.CuentaDeAna.SubirAsync(ruta, SembradorFichas.TextoDisfrazado(), "documento.pdf"), "archivo_no_admitido"),
            ("vacío", await e.CuentaDeAna.SubirAsync(ruta, []), "archivo_no_admitido"),
            ("sin archivo", await e.CuentaDeAna.SubirSinArchivoAsync(ruta), "archivo_no_admitido"),
            ("de más de 10 MB", await e.CuentaDeAna.SubirAsync(ruta, SembradorFichas.Pdf(ValidadorArchivoDeFicha.TamanoMaximo + 1)), "archivo_demasiado_grande"),
        };

        foreach (var (caso, respuesta, codigo) in rechazos)
        {
            Assert.True(respuesta.StatusCode == HttpStatusCode.BadRequest, $"{caso}: {(int)respuesta.StatusCode}");
            Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
        }

        var abierto = await e.CuentaDeAna.GetAsync(ruta);
        if (habiaUno)
        {
            Assert.Equal(anterior, await abierto.Content.ReadAsByteArrayAsync());
        }
        else
        {
            Assert.Equal(HttpStatusCode.NotFound, abierto.StatusCode);
            Assert.Empty(await e.DocumentosGuardadosAsync(e.Ana.Id));
        }

        // Un rechazo no es un cambio de la ficha.
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    [Fact]
    public async Task Admite_un_archivo_de_exactamente_10_MB()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeAna.SubirAsync(
            e.Documento(e.Ana.Id, Copia), SembradorFichas.Pdf(ValidadorArchivoDeFicha.TamanoMaximo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(
            ValidadorArchivoDeFicha.TamanoMaximo,
            (await respuesta.JsonAsync()).DocumentoDe(Copia).GetProperty("tamanoBytes").GetInt32());
    }

    [Fact]
    public async Task Abrir_un_documento_pendiente_responde_404()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeAna.GetAsync(e.Documento(e.Ana.Id, Certificado));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    // La lista es fija (RF-027): lo que no es exactamente uno de sus nombres no existe.
    [Theory]
    [InlineData("OTRO_DOCUMENTO")]
    [InlineData("certificado_salud")]
    [InlineData("0")]
    [InlineData("1")]
    public async Task Un_documento_que_no_es_de_la_lista_responde_404_al_abrir_y_al_subir(string documento)
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, SembradorFichas.Pdf(), "application/pdf");
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Certificado, SembradorFichas.Pdf(), "application/pdf");
        var ruta = $"{e.Ficha(e.Ana.Id)}/documentos/{documento}";

        var abierto = await e.CuentaDeAna.GetAsync(ruta);
        var subida = await e.CuentaDeAna.SubirAsync(ruta, SembradorFichas.Png());

        Assert.Equal(HttpStatusCode.NotFound, abierto.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, subida.StatusCode);
        Assert.All(await e.DocumentosGuardadosAsync(e.Ana.Id), archivo => Assert.Equal("application/pdf", archivo.TipoContenido));
    }

    // Escenario 3.1: sin archivos, los dos pedidos y pendientes; el contenido nunca viaja en la ficha.
    [Fact]
    public async Task La_ficha_trae_el_estado_de_los_documentos_y_nunca_su_contenido()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var pendientes = await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id);
        Assert.All(pendientes.Lista("documentos"), documento => Assert.False(documento.GetProperty("entregado").GetBoolean()));

        await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Copia), SembradorFichas.Pdf());
        var respuesta = await e.CuentaDeAna.GetAsync(e.Ficha(e.Ana.Id));

        var documento = (await respuesta.JsonAsync()).DocumentoDe(Copia);
        Assert.Equal(
            ["documento", "entregado", "subidoEn", "tamanoBytes", "tipoContenido"],
            documento.EnumerateObject().Select(propiedad => propiedad.Name).Order(StringComparer.Ordinal));
        Assert.Equal(JsonValueKind.String, documento.GetProperty("subidoEn").ValueKind);
        Assert.DoesNotContain("contenido\"", await respuesta.Content.ReadAsStringAsync());
    }
}
