using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// Los archivos de la ficha los abren solo la cuenta del jugador, el PRESIDENTE y los DIRECTIVOS
/// de su club, y los suben solo la cuenta del jugador y el PRESIDENTE (historia 3, escenarios 5, 7
/// y 8; RF-007, RF-011, RF-019, RF-031; CE-002 y CE-003).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AccesoADocumentosPruebas
{
    private const DocumentoPedido Copia = DocumentoPedido.COPIA_DOCUMENTO_IDENTIDAD;

    private readonly FabricaApi _fabrica;

    public AccesoADocumentosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    // Escenario 3.5: de cualquier jugador del club, también retirado o sin categoría.
    [Fact]
    public async Task El_presidente_y_el_directivo_abren_el_archivo_de_cualquier_jugador_del_club()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var contenido = SembradorFichas.Pdf(64);

        foreach (var jugador in new[] { e.Ana, e.Caro, e.Dani })
        {
            await _fabrica.Fichas.CrearDocumentoAsync(jugador, Copia, contenido, "application/pdf");

            foreach (var cliente in new[] { e.Presidente, e.Directivo })
            {
                var respuesta = await cliente.GetAsync(e.Documento(jugador.Id, Copia));

                Assert.True(respuesta.StatusCode == HttpStatusCode.OK, $"{jugador.Nombres}: {(int)respuesta.StatusCode}");
                Assert.Equal(contenido, await respuesta.Content.ReadAsByteArrayAsync());
                Assert.True((await e.FichaDeAsync(cliente, jugador.Id)).DocumentoDe(Copia).GetProperty("entregado").GetBoolean());
            }
        }
    }

    [Fact]
    public async Task El_directivo_recibe_403_al_subir_y_no_cambia_nada()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.Directivo.SubirAsync(e.Documento(e.Ana.Id, Copia), SembradorFichas.Pdf());

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Empty(await e.DocumentosGuardadosAsync(e.Ana.Id));
    }

    // Escenario 3.7 y CE-003: tampoco el entrenador de la categoría del jugador.
    [Fact]
    public async Task El_entrenador_de_la_categoria_recibe_403_al_abrir_y_al_subir()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var contenido = SembradorFichas.Pdf(64);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, contenido, "application/pdf");
        var ruta = e.Documento(e.Ana.Id, Copia);

        var abierto = await e.Entrenador.GetAsync(ruta);
        var subida = await e.Entrenador.SubirAsync(ruta, SembradorFichas.Png());

        foreach (var respuesta in new[] { abierto, subida })
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        // Responde igual para un jugador que no existe: no revela nada.
        var inexistente = await e.Entrenador.GetAsync(e.Documento(Guid.NewGuid(), Copia));
        Assert.Equal(await abierto.Content.ReadAsStringAsync(), await inexistente.Content.ReadAsStringAsync());
        Assert.Equal(contenido, Assert.Single(await e.DocumentosGuardadosAsync(e.Ana.Id)).Contenido);
    }

    // Escenario 3.8 y CE-002.
    [Fact]
    public async Task Otra_cuenta_de_jugador_recibe_404_al_abrir_y_al_subir_y_nada_cambia()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var contenido = SembradorFichas.Pdf(64);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, contenido, "application/pdf");
        var ruta = e.Documento(e.Ana.Id, Copia);

        var abierto = await e.CuentaDeBeto.GetAsync(ruta);
        var subida = await e.CuentaDeBeto.SubirAsync(ruta, SembradorFichas.Png());

        foreach (var respuesta in new[] { abierto, subida })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Equal(contenido, Assert.Single(await e.DocumentosGuardadosAsync(e.Ana.Id)).Contenido);
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    // RF-011 y CE-006.
    [Fact]
    public async Task El_presidente_de_otro_club_y_el_desarrollador_reciben_404()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, SembradorFichas.Pdf(64), "application/pdf");
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var enElOtroClub = $"{EscenarioFicha.Ficha(e.Base.OtroClub.Id, e.Ana.Id)}/documentos/{Copia}";

        var respuestas = new[]
        {
            await e.Base.PresidenteDeOtroClub.GetAsync(e.Documento(e.Ana.Id, Copia)),
            await e.Base.PresidenteDeOtroClub.GetAsync(enElOtroClub),
            await e.Base.PresidenteDeOtroClub.SubirAsync(enElOtroClub, SembradorFichas.Png()),
            await desarrollador.GetAsync(e.Documento(e.Ana.Id, Copia)),
            await desarrollador.SubirAsync(e.Documento(e.Ana.Id, Copia), SembradorFichas.Png()),
        };

        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Equal("application/pdf", Assert.Single(await e.DocumentosGuardadosAsync(e.Ana.Id)).TipoContenido);
    }

    [Fact]
    public async Task Sin_sesion_el_archivo_responde_401()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, SembradorFichas.Pdf(64), "application/pdf");

        var respuesta = await _fabrica.CrearClienteDePrueba().GetAsync(e.Documento(e.Ana.Id, Copia));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(respuesta));
    }
}
