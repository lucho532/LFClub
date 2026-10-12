using System.Net;
using System.Text;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Pruebas.Integracion.Base;
using SkiaSharp;
using static LaPecosa.Pruebas.Integracion.Carta.ConsultaCartaPruebas;

namespace LaPecosa.Pruebas.Integracion.Carta;

/// <summary>
/// La imagen de la carta solo la obtienen la familia del jugador y el PRESIDENTE, también la de un
/// retirado; nadie más, ni pidiéndola directamente (spec 010, escenarios 4.1 a 4.4; RF-029 a RF-031).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ExportarCartaPruebas
{
    private readonly FabricaApi _fabrica;

    public ExportarCartaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Imagen(EscenarioActividades e, Persona jugador) => $"{RutaCarta(e, jugador)}/imagen";

    private static async Task<byte[]> PngAsync(HttpResponseMessage respuesta, Persona jugador)
    {
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, $"{(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
        Assert.Equal("image/png", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.True(respuesta.Headers.CacheControl is { Private: true, NoStore: true });
        var disposicion = respuesta.Content.Headers.ContentDisposition;
        Assert.Equal("attachment", disposicion?.DispositionType);
        var esperado = $"carta-{NormalizadorTexto.ParaArchivo(jugador.Integrante.Apellidos)}-{NormalizadorTexto.ParaArchivo(jugador.Integrante.Nombres)}.png";
        Assert.Equal(esperado, disposicion?.FileName?.Trim('"'));
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        using var imagen = SKBitmap.Decode(bytes);
        Assert.Equal((750, 1050), (imagen.Width, imagen.Height));
        return bytes;
    }

    [Fact]
    public async Task La_familia_y_el_presidente_la_descargan()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Cartas.PonerColoresYEscudoAsync(e.Club, "#1B3A6B", "#F2C200");
        await _fabrica.Cartas.PonerFotoAsync(e.Jn(3).Integrante);

        var bytes = await PngAsync(await e.Jn(3).Cliente.GetAsync(Imagen(e, e.Jn(3))), e.Jn(3));
        await PngAsync(await e.P.Cliente.GetAsync(Imagen(e, e.Jn(4))), e.Jn(4));

        // RF-030: la imagen no lleva el documento, el correo ni el celular del jugador.
        var texto = Encoding.Latin1.GetString(bytes);
        Assert.DoesNotContain(e.Jn(3).Integrante.NumeroDocumento, texto);
        Assert.DoesNotContain(e.Jn(3).Cuenta.Correo, texto);
        if (e.Jn(3).Cuenta.Celular is { } celular)
        {
            Assert.DoesNotContain(celular, texto);
        }
    }

    [Fact]
    public async Task El_presidente_descarga_la_de_un_retirado_y_su_familia_ya_no_llega()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Categorias.RetirarAsync(e.Jn(8).Integrante, e.P.Integrante);

        await PngAsync(await e.P.Cliente.GetAsync(Imagen(e, e.Jn(8))), e.Jn(8));
        await Actividades.ErrorAsync(await e.Jn(8).Cliente.GetAsync(Imagen(e, e.Jn(8))), HttpStatusCode.Forbidden, "integrante_retirado");
    }

    [Fact]
    public async Task Quien_entrena_o_dirige_no_la_obtiene_aunque_vea_la_carta()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        foreach (var persona in new[] { e.E1, e.D1 })
        {
            await Actividades.LeerAsync(await persona.Cliente.GetAsync(RutaCarta(e, e.Jn(3))));
            await Actividades.ErrorAsync(await persona.Cliente.GetAsync(Imagen(e, e.Jn(3))), HttpStatusCode.Forbidden, "rol_no_autorizado");
        }
    }

    [Fact]
    public async Task Otra_familia_otro_club_o_el_desarrollador_no_la_encuentran()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        foreach (var cliente in new[] { e.Jn(4).Cliente, e.PB.Cliente, desarrollador })
        {
            await Actividades.ErrorAsync(await cliente.GetAsync(Imagen(e, e.Jn(3))), HttpStatusCode.NotFound, "no_encontrado");
        }
    }
}
