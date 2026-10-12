using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// Una cuenta de jugador no obtiene ni cambia nada de otro jugador, solo los jugadores tienen ficha
/// y el retirado no accede a la suya (historia 1, escenarios 7 y 8; RF-004, RF-005, RF-012,
/// RF-013; CE-002).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FichaAjenaPruebas
{
    private readonly FabricaApi _fabrica;

    public FichaAjenaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_recibe_404_ante_la_ficha_de_otro_jugador_de_su_club()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Beto);

        var respuesta = await e.CuentaDeAna.GetAsync(e.Ficha(e.Beto.Id));
        var inexistente = await e.CuentaDeAna.GetAsync(e.Ficha(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));

        // El mismo cuerpo que para un identificador que no existe: no se revela que la ficha existe (RF-012).
        Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_recibe_404_ante_la_ficha_de_un_jugador_de_otro_club()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, ajeno) = await e.Base.Sembrar.CrearJugadorAsync(e.Base.OtroClub, 2014);
        await _fabrica.Fichas.CrearFichaAsync(ajeno);

        var enSuClub = await e.CuentaDeAna.GetAsync(e.Ficha(ajeno.Id));
        var enElOtro = await e.CuentaDeAna.GetAsync(EscenarioFicha.Ficha(e.Base.OtroClub.Id, ajeno.Id));

        foreach (var respuesta in new[] { enSuClub, enElOtro })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(SembradorFichas.Alergias, await respuesta.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Guardar_sobre_la_ficha_de_otro_jugador_responde_404_y_nada_cambia()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeAna.PutAsync(e.Ficha(e.Beto.Id), EscenarioFicha.Cuerpo());

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Null(await e.FichaGuardadaAsync(e.Beto.Id));
        Assert.Equal("3001234567", (await e.JugadorGuardadoAsync(e.Beto.Id)).Usuario!.Celular);
    }

    // RF-004: quien no es JUGADOR, o todavía está en espera, no tiene ficha; tampoco para el PRESIDENTE.
    [Fact]
    public async Task Solo_un_jugador_aprobado_tiene_ficha()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, enEspera) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(e.Club);
        var sinFicha = new Dictionary<string, Guid>
        {
            ["ENTRENADOR"] = e.Base.IntegranteEntrenador.Id,
            ["DIRECTIVO"] = e.Base.IntegranteDirectivo.Id,
            ["PRESIDENTE"] = e.Base.IntegrantePresidente.Id,
            ["en espera"] = enEspera.Id,
        };

        foreach (var (quien, id) in sinFicha)
        {
            var lectura = await e.Presidente.GetAsync(e.Ficha(id));
            var cambio = await e.Presidente.PutAsync(e.Ficha(id), EscenarioFicha.Cuerpo());

            Assert.True(lectura.StatusCode == HttpStatusCode.NotFound, $"GET {quien}: {(int)lectura.StatusCode}");
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(lectura));
            Assert.True(cambio.StatusCode == HttpStatusCode.NotFound, $"PUT {quien}: {(int)cambio.StatusCode}");
            Assert.Null(await e.FichaGuardadaAsync(id));
        }
    }

    // Escenario 1.8 y RF-013.
    [Fact]
    public async Task El_jugador_retirado_recibe_403_y_ningun_dato_de_su_ficha()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Dani);

        var lectura = await e.CuentaDeDani.GetAsync(e.Ficha(e.Dani.Id));
        var cambio = await e.CuentaDeDani.PutAsync(e.Ficha(e.Dani.Id), EscenarioFicha.Cuerpo());

        foreach (var respuesta in new[] { lectura, cambio })
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(SembradorFichas.Alergias, await respuesta.Content.ReadAsStringAsync());
        }

        Assert.Equal(SembradorFichas.EntidadSalud, (await e.FichaGuardadaAsync(e.Dani.Id))!.EntidadSalud);
    }

    [Fact]
    public async Task Sin_sesion_la_ficha_responde_401()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await _fabrica.CrearClienteDePrueba().GetAsync(e.Ficha(e.Ana.Id));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    // Caso límite: "Mi ficha" es de la cuenta del jugador; un jugador no abre la de un compañero de categoría.
    [Fact]
    public async Task Un_jugador_no_ve_la_ficha_de_un_companero_de_su_misma_categoria()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, companero) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 2014, e.CategoriaDeAna, "Eva");

        var respuesta = await e.CuentaDeAna.GetAsync(e.Ficha(companero.Id));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(Rol.JUGADOR, companero.Rol);
    }
}
