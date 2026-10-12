using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// El ENTRENADOR ve la ficha, con los datos clínicos y sin los documentos, solo de los jugadores
/// activos de las categorías que entrena, y pierde el acceso al instante (historia 2, escenarios
/// 2, 3 y 8; RF-007, RF-008; CE-003).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AlcanceDelEntrenadorPruebas
{
    private readonly FabricaApi _fabrica;

    public AlcanceDelEntrenadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    // Escenario 2.2: de cualquier equipo, lo dirija él o no.
    [Fact]
    public async Task Recibe_la_ficha_de_un_jugador_de_su_categoria_con_datos_clinicos_y_sin_documentos()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var equipo = await e.Base.Sembrar.CrearEquipoAsync(e.CategoriaDeAna, "B");
        await e.Base.Sembrar.PonerEnEquipoAsync(e.Ana, equipo);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        await _fabrica.Fichas.CrearDocumentoAsync(
            e.Ana, DocumentoPedido.CERTIFICADO_SALUD, SembradorFichas.Pdf(), "application/pdf");

        var respuesta = await e.Entrenador.GetAsync(e.Ficha(e.Ana.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var ficha = await respuesta.JsonAsync();
        Assert.Equal(e.Ana.NumeroDocumento, ficha.GetProperty("numeroDocumento").GetString());
        Assert.Equal(["B"], ficha.Lista("equipos").Select(nombre => nombre.GetString()));
        Assert.Equal(SembradorFichas.EmergenciaNombre, ficha.GetProperty("contactoEmergencia").GetProperty("nombre").GetString());
        Assert.Equal(SembradorFichas.LugarAtencion, ficha.GetProperty("seguridadSocial").GetProperty("lugarAtencion").GetString());
        Assert.Equal("O_NEGATIVO", ficha.GetProperty("datosClinicos").GetProperty("grupoSanguineo").GetString());
        Assert.Equal(SembradorFichas.Medicamentos, ficha.GetProperty("datosClinicos").GetProperty("medicamentos").GetString());

        // No sabe si los documentos están entregados o pendientes (RF-007).
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.False(ficha.Tiene("documentos"));
        Assert.DoesNotContain("documentos", cuerpo);
        Assert.DoesNotContain("CERTIFICADO_SALUD", cuerpo);
        Assert.DoesNotContain("application/pdf", cuerpo);
    }

    // Escenario 2.3 y RF-008.
    [Fact]
    public async Task Recibe_404_ante_un_jugador_de_otra_categoria_uno_sin_categoria_y_uno_retirado()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var inexistente = await (await e.Entrenador.GetAsync(e.Ficha(Guid.NewGuid()))).Content.ReadAsStringAsync();

        foreach (var jugador in new[] { e.Beto, e.Caro, e.Dani })
        {
            await _fabrica.Fichas.CrearFichaAsync(jugador);

            var respuesta = await e.Entrenador.GetAsync(e.Ficha(jugador.Id));

            Assert.True(respuesta.StatusCode == HttpStatusCode.NotFound, $"{jugador.Nombres}: {(int)respuesta.StatusCode}");
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.Equal(inexistente, await respuesta.Content.ReadAsStringAsync());
        }
    }

    // Escenario 2.8: sin cerrar sesión ninguno de los dos.
    [Fact]
    public async Task Al_cambiar_el_jugador_de_categoria_lo_deja_de_ver_el_entrenador_anterior_y_lo_ve_el_nuevo()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (cuentaNuevo, nuevo) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.ENTRENADOR);
        await e.Base.Sembrar.AsignarEntrenadorAsync(e.CategoriaDeBeto, nuevo);
        var entrenadorNuevo = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaNuevo);
        Assert.Equal(HttpStatusCode.OK, (await e.Entrenador.GetAsync(e.Ficha(e.Ana.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await entrenadorNuevo.GetAsync(e.Ficha(e.Ana.Id))).StatusCode);

        var cambio = await e.Presidente.PutAsync(e.Base.CategoriaDe(e.Ana.Id), new { categoriaId = e.CategoriaDeBeto.Id });

        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Entrenador.GetAsync(e.Ficha(e.Ana.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await entrenadorNuevo.GetAsync(e.Ficha(e.Ana.Id))).StatusCode);
    }

    // Caso límite: pierde el acceso de inmediato.
    [Fact]
    public async Task Al_retirarle_la_asignacion_recibe_404_de_inmediato()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        Assert.Equal(HttpStatusCode.OK, (await e.Entrenador.GetAsync(e.Ficha(e.Ana.Id))).StatusCode);

        var retiro = await e.Presidente.DeleteAsync(e.Base.EntrenadorDe(e.CategoriaDeAna.Id, e.Base.IntegranteEntrenador.Id));

        Assert.True(retiro.IsSuccessStatusCode);
        var respuesta = await e.Entrenador.GetAsync(e.Ficha(e.Ana.Id));
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Con_la_categoria_inactiva_recibe_404()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var inactiva = await e.Base.Sembrar.CrearCategoriaAsync(e.Club, 2013, activa: false);
        await e.Base.Sembrar.AsignarEntrenadorAsync(inactiva, e.Base.IntegranteEntrenador);
        var (_, jugador) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 2013, inactiva);

        var respuesta = await e.Entrenador.GetAsync(e.Ficha(jugador.Id));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.GetAsync(e.Ficha(jugador.Id))).StatusCode);
    }

    // Una asignación que ya no está activa no da acceso.
    [Fact]
    public async Task Con_una_asignacion_inactiva_recibe_404()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.Base.Sembrar.AsignarEntrenadorAsync(e.CategoriaDeBeto, e.Base.IntegranteEntrenador, activa: false);

        Assert.Equal(HttpStatusCode.NotFound, (await e.Entrenador.GetAsync(e.Ficha(e.Beto.Id))).StatusCode);
    }
}
