using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// El PRESIDENTE y los DIRECTIVOS ven en las listas de jugadores cuántos documentos le faltan a
/// cada uno; el ENTRENADOR no recibe ese dato (historia 3, escenarios 6 y 7; RF-007, RF-032;
/// CE-009).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class DocumentacionEnListasPruebas
{
    private const DocumentoPedido Copia = DocumentoPedido.COPIA_DOCUMENTO_IDENTIDAD;
    private const DocumentoPedido Certificado = DocumentoPedido.CERTIFICADO_SALUD;

    private readonly FabricaApi _fabrica;

    public DocumentacionEnListasPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    // Escenario 3.6: sin archivos, con uno y con los dos.
    [Fact]
    public async Task El_presidente_y_el_directivo_reciben_cuantos_documentos_faltan_en_el_detalle_de_la_categoria()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, conUno) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 2014, e.CategoriaDeAna, "Uno");
        var (_, conDos) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 2014, e.CategoriaDeAna, "Dos");
        await EntregarAsync(conUno, Copia);
        await EntregarAsync(conDos, Copia, Certificado);

        foreach (var cliente in new[] { e.Presidente, e.Directivo })
        {
            var pendientes = await PendientesAsync(cliente, e.Base.RutaCategoria(e.CategoriaDeAna.Id));

            Assert.Equal(2, pendientes[e.Ana.Id]);
            Assert.Equal(1, pendientes[conUno.Id]);
            Assert.Equal(0, pendientes[conDos.Id]);
        }
    }

    [Fact]
    public async Task Las_listas_sin_categoria_y_retirados_traen_el_estado_de_cada_jugador()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, otroSinCategoria) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 2019);
        await EntregarAsync(e.Caro, Certificado);
        await EntregarAsync(e.Dani, Copia, Certificado);

        foreach (var cliente in new[] { e.Presidente, e.Directivo })
        {
            var sinCategoria = (await cliente.ListaAsync(e.Base.SinCategoria))
                .ToDictionary(Id, jugador => jugador.GetProperty("documentosPendientes").GetInt32());
            var retirados = (await cliente.ListaAsync(e.Base.Retirados))
                .ToDictionary(Id, jugador => jugador.GetProperty("documentosPendientes").GetInt32());

            Assert.Equal(1, sinCategoria[e.Caro.Id]);
            Assert.Equal(2, sinCategoria[otroSinCategoria.Id]);
            Assert.Equal(0, retirados[e.Dani.Id]);
        }
    }

    // CE-009: no hay contador guardado; el número sale de los archivos que hay.
    [Fact]
    public async Task El_numero_cambia_al_subir_un_archivo_y_no_al_reemplazarlo()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var ruta = e.Base.RutaCategoria(e.CategoriaDeAna.Id);
        Assert.Equal(2, (await PendientesAsync(e.Directivo, ruta))[e.Ana.Id]);

        await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Copia), SembradorFichas.Png());
        Assert.Equal(1, (await PendientesAsync(e.Directivo, ruta))[e.Ana.Id]);

        await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Copia), SembradorFichas.Pdf());
        Assert.Equal(1, (await PendientesAsync(e.Directivo, ruta))[e.Ana.Id]);

        await e.Presidente.SubirAsync(e.Documento(e.Ana.Id, Certificado), SembradorFichas.Pdf());
        Assert.Equal(0, (await PendientesAsync(e.Directivo, ruta))[e.Ana.Id]);
    }

    // Escenario 3.7 y RF-007: la propiedad no existe en ningún jugador.
    [Fact]
    public async Task El_entrenador_recibe_el_detalle_de_su_categoria_sin_el_estado_de_la_documentacion()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await EntregarAsync(e.Ana, Copia);

        var respuesta = await e.Entrenador.GetAsync(e.Base.RutaCategoria(e.CategoriaDeAna.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var jugadores = (await respuesta.JsonAsync()).Lista("jugadores");
        Assert.Contains(e.Ana.Id, jugadores.Ids("usuarioRolId"));
        Assert.All(jugadores, jugador => Assert.False(jugador.Tiene("documentosPendientes")));
        Assert.DoesNotContain("documentosPendientes", await respuesta.Content.ReadAsStringAsync());
    }

    // Un directivo asignado como entrenador sigue viéndolo: lo decide su rol, no la asignación.
    [Fact]
    public async Task El_directivo_asignado_como_entrenador_sigue_recibiendo_el_estado()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.Base.Sembrar.AsignarEntrenadorAsync(e.CategoriaDeAna, e.Base.IntegranteDirectivo);

        Assert.Equal(2, (await PendientesAsync(e.Directivo, e.Base.RutaCategoria(e.CategoriaDeAna.Id)))[e.Ana.Id]);
    }

    // Las operaciones de la 003 que devuelven el detalle, todas de PRESIDENTE, lo siguen trayendo.
    [Fact]
    public async Task Ubicar_a_un_jugador_en_un_equipo_devuelve_el_detalle_con_el_estado_de_la_documentacion()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var equipo = await e.Base.Sembrar.CrearEquipoAsync(e.CategoriaDeAna, "A");
        await EntregarAsync(e.Ana, Copia);

        var respuesta = await e.Presidente.PutAsync(e.Base.JugadorDeEquipo(e.CategoriaDeAna.Id, equipo.Id, e.Ana.Id), new { });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var ana = (await respuesta.JsonAsync()).Lista("jugadores").Single(jugador => Id(jugador) == e.Ana.Id);
        Assert.Equal(1, ana.GetProperty("documentosPendientes").GetInt32());
        Assert.Equal([equipo.Id], ana.Lista("equipos").Ids("equipoId"));
    }

    private static Guid Id(JsonElement jugador) => jugador.GetProperty("usuarioRolId").GetGuid();

    private static async Task<Dictionary<Guid, int>> PendientesAsync(ClienteDePrueba cliente, string rutaDeCategoria) =>
        (await (await cliente.GetAsync(rutaDeCategoria)).JsonAsync()).Lista("jugadores")
            .ToDictionary(Id, jugador => jugador.GetProperty("documentosPendientes").GetInt32());

    private async Task EntregarAsync(UsuarioRol jugador, params DocumentoPedido[] documentos)
    {
        foreach (var documento in documentos)
        {
            await _fabrica.Fichas.CrearDocumentoAsync(jugador, documento, SembradorFichas.Pdf(), "application/pdf");
        }
    }
}
