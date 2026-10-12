using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// El club consulta la ficha según el rol (historia 2, escenarios 1, 4 a 6 y 9 a 11; RF-006,
/// RF-009 a RF-012; CE-004 y CE-006): el PRESIDENTE la ve completa y el DIRECTIVO nunca recibe los
/// datos clínicos, tampoco asignado como entrenador.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConsultaPorRolPruebas
{
    private static readonly string[] GruposQueVeElDirectivo = ["contacto", "contactoEmergencia", "seguridadSocial", "documentos"];

    private readonly FabricaApi _fabrica;

    public ConsultaPorRolPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    // Escenario 2.1: con categoría, sin categoría y retirado.
    [Fact]
    public async Task El_presidente_recibe_la_ficha_completa_de_cualquier_jugador_de_su_club()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        foreach (var jugador in new[] { e.Ana, e.Caro, e.Dani })
        {
            await _fabrica.Fichas.CrearFichaAsync(jugador);

            var ficha = await e.FichaDeAsync(e.Presidente, jugador.Id);

            Assert.Equal(jugador.Nombres, ficha.GetProperty("nombres").GetString());
            Assert.Equal(jugador.Id == e.Dani.Id, ficha.GetProperty("retirado").GetBoolean());
            Assert.Equal(SembradorFichas.Alergias, ficha.GetProperty("datosClinicos").GetProperty("alergias").GetString());
            Assert.Equal(SembradorFichas.EmergenciaNombre, ficha.GetProperty("contactoEmergencia").GetProperty("nombre").GetString());
            Assert.Equal(2, ficha.Lista("documentos").Count);
            Assert.True(ficha.GetProperty("permisos").GetProperty("puedeCambiar").GetBoolean());
            Assert.True(ficha.GetProperty("permisos").GetProperty("puedeCorregirIdentidad").GetBoolean());
        }
    }

    // Escenarios 2.4 y 2.5 y CE-004: ni la propiedad ni ninguno de los textos clínicos, en ninguna parte.
    [Fact]
    public async Task El_directivo_recibe_todo_menos_los_datos_clinicos_de_cualquier_jugador()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        foreach (var jugador in new[] { e.Ana, e.Caro, e.Dani })
        {
            await _fabrica.Fichas.CrearFichaAsync(jugador);

            var respuesta = await e.Directivo.GetAsync(e.Ficha(jugador.Id));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var ficha = await respuesta.JsonAsync();
            Assert.Equal(jugador.NumeroDocumento, ficha.GetProperty("numeroDocumento").GetString());
            Assert.All(GruposQueVeElDirectivo, grupo => Assert.True(ficha.Tiene(grupo), grupo));
            Assert.Equal(SembradorFichas.EmergenciaCelular, ficha.GetProperty("contactoEmergencia").GetProperty("celular").GetString());
            Assert.Equal(SembradorFichas.EntidadSalud, ficha.GetProperty("seguridadSocial").GetProperty("entidadSalud").GetString());
            Assert.True(ficha.Tiene("ultimoCambio"));
            SinDatosClinicos(ficha, await respuesta.Content.ReadAsStringAsync());
        }
    }

    // Escenario 2.6 y RF-010: la asignación no le da nada.
    [Fact]
    public async Task El_directivo_asignado_como_entrenador_de_la_categoria_recibe_exactamente_lo_mismo()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        var antes = await (await e.Directivo.GetAsync(e.Ficha(e.Ana.Id))).Content.ReadAsStringAsync();

        await e.Base.Sembrar.AsignarEntrenadorAsync(e.CategoriaDeAna, e.Base.IntegranteDirectivo);
        var respuesta = await e.Directivo.GetAsync(e.Ficha(e.Ana.Id));

        var despues = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(antes, despues);
        SinDatosClinicos(await respuesta.JsonAsync(), despues);
    }

    [Fact]
    public async Task El_presidente_asignado_como_entrenador_sigue_viendo_todo_tambien_fuera_de_esa_categoria()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.Base.Sembrar.AsignarEntrenadorAsync(e.CategoriaDeAna, e.Base.IntegrantePresidente);

        foreach (var jugador in new[] { e.Ana, e.Beto, e.Caro, e.Dani })
        {
            var ficha = await e.FichaDeAsync(e.Presidente, jugador.Id);

            Assert.True(ficha.Tiene("datosClinicos"));
            Assert.True(ficha.Tiene("documentos"));
            Assert.True(ficha.GetProperty("permisos").GetProperty("puedeCorregirIdentidad").GetBoolean());
        }
    }

    [Fact]
    public async Task Los_permisos_del_directivo_y_del_entrenador_llegan_en_falso()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        foreach (var (rol, cliente) in e.QuienesSoloConsultan)
        {
            var permisos = (await e.FichaDeAsync(cliente, e.Ana.Id)).GetProperty("permisos");

            Assert.False(permisos.GetProperty("puedeCambiar").GetBoolean(), rol);
            Assert.False(permisos.GetProperty("puedeCorregirIdentidad").GetBoolean(), rol);
        }
    }

    // Escenario 2.9 y CE-006: ni con el identificador en su propia ruta ni en la del club ajeno.
    [Fact]
    public async Task El_presidente_de_otro_club_recibe_404_en_su_ruta_y_en_la_del_club_ajeno()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Beto);
        var ajeno = e.Base.PresidenteDeOtroClub;

        var enSuClub = await ajeno.GetAsync(EscenarioFicha.Ficha(e.Base.OtroClub.Id, e.Beto.Id));
        var enElAjeno = await ajeno.GetAsync(e.Ficha(e.Beto.Id));

        foreach (var respuesta in new[] { enSuClub, enElAjeno })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            Assert.DoesNotContain(e.Beto.Apellidos, cuerpo);
            Assert.DoesNotContain(SembradorFichas.EntidadSalud, cuerpo);
        }
    }

    // Escenario 2.10 y RF-011.
    [Fact]
    public async Task El_desarrollador_recibe_404()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await desarrollador.GetAsync(e.Ficha(e.Ana.Id));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.DoesNotContain(SembradorFichas.Alergias, await respuesta.Content.ReadAsStringAsync());
    }

    // Escenario 2.11.
    [Fact]
    public async Task Sin_sesion_responde_401_y_ningun_dato()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);

        var respuesta = await _fabrica.CrearClienteDePrueba().GetAsync(e.Ficha(e.Ana.Id));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("sin_sesion", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.DoesNotContain(e.Ana.NumeroDocumento, await respuesta.Content.ReadAsStringAsync());
    }

    private static void SinDatosClinicos(JsonElement ficha, string cuerpo)
    {
        Assert.False(ficha.Tiene("datosClinicos"));
        Assert.DoesNotContain("datosClinicos", cuerpo);
        Assert.DoesNotContain("grupoSanguineo", cuerpo);
        Assert.All(SembradorFichas.TextosClinicos, texto => Assert.DoesNotContain(texto, cuerpo));
    }
}
