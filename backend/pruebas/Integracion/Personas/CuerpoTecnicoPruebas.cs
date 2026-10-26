using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LFClub.Pruebas.Integracion.Personas;

/// <summary>El cuerpo técnico del club y los datos de cada persona, solo para el PRESIDENTE (spec 018).</summary>
[Collection(ColeccionApi.Nombre)]
public class CuerpoTecnicoPruebas
{
    private static DateOnly EnDias(int dias) => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(dias);

    private readonly FabricaApi _fabrica;

    public CuerpoTecnicoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Lista(EscenarioActividades c) => c.Ruta("/cuerpo-tecnico");

    private static string Ficha(EscenarioActividades c, Persona persona) => c.Ruta($"/cuerpo-tecnico/{persona.Id}");

    [Fact]
    public async Task El_presidente_ve_a_su_cuerpo_tecnico_por_rol_con_sus_categorias_y_sin_jugadores()
    {
        var c = await _fabrica.Actividades.CrearClubAsync();

        var lista = (await LeerAsync(c.P.Cliente, Lista(c))).EnumerateArray().ToList();

        Assert.Equal(5, lista.Count);
        Assert.Equal(["PRESIDENTE", "DIRECTIVO", "DIRECTIVO", "ENTRENADOR", "ENTRENADOR"], lista.Select(m => m.GetProperty("rol").GetString()).ToList());
        var e2 = lista.Single(m => m.GetProperty("usuarioRolId").GetGuid() == c.E2.Id);
        Assert.Equal(["2014: ", "2015: "], CategoriasDe(e2));
        Assert.Equal(c.E2.Integrante.Nombres, e2.GetProperty("nombres").GetString());
        var d2 = lista.Single(m => m.GetProperty("usuarioRolId").GetGuid() == c.D2.Id);
        Assert.Empty(d2.GetProperty("categorias").EnumerateArray());
        Assert.DoesNotContain(lista, m => m.GetProperty("usuarioRolId").GetGuid() == c.Jn(1).Id);
    }

    [Fact]
    public async Task La_lista_no_trae_documento_ni_contacto()
    {
        var c = await _fabrica.Actividades.CrearClubAsync();

        var respuesta = await c.P.Cliente.GetAsync(Lista(c));
        var texto = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("no-store", respuesta.Headers.CacheControl?.ToString());
        Assert.DoesNotContain("numeroDocumento", texto);
        Assert.DoesNotContain("correo", texto);
        Assert.DoesNotContain("celular", texto);
    }

    [Fact]
    public async Task El_detalle_de_un_entrenador_trae_sus_datos_sus_categorias_y_los_proximos_entrenamientos_de_ellas()
    {
        var c = await _fabrica.Actividades.CrearClubAsync();
        var de2014 = await _fabrica.Actividades.CrearEntrenamientoAsync([c.C2014], EnDias(3));
        var de2015 = await _fabrica.Actividades.CrearEntrenamientoAsync([c.C2015], EnDias(4));
        var de2013 = await _fabrica.Actividades.CrearEntrenamientoAsync([c.C2013], EnDias(5));
        var pasado = await _fabrica.Actividades.CrearEntrenamientoAsync([c.C2014], EnDias(-3));

        var respuesta = await c.P.Cliente.GetAsync(Ficha(c, c.E2));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no-store", respuesta.Headers.CacheControl?.ToString());
        var ficha = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(c.E2.Integrante.Nombres, ficha.GetProperty("nombres").GetString());
        Assert.Equal(c.E2.Integrante.Apellidos, ficha.GetProperty("apellidos").GetString());
        Assert.Equal("ENTRENADOR", ficha.GetProperty("rol").GetString());
        Assert.Equal(c.E2.Integrante.NumeroDocumento, ficha.GetProperty("numeroDocumento").GetString());
        Assert.Equal(c.E2.Integrante.TipoDocumento.ToString(), ficha.GetProperty("tipoDocumento").GetString());
        Assert.Equal(c.E2.Cuenta.Correo, ficha.GetProperty("correo").GetString());
        Assert.Equal(["2014: ", "2015: "], CategoriasDe(ficha));
        var ids = ficha.GetProperty("proximosEntrenamientos").EnumerateArray()
            .Select(e => e.GetProperty("entrenamientoId").GetGuid()).ToList();
        Assert.Equal([de2014.Id, de2015.Id], ids);
        Assert.DoesNotContain(de2013.Id, ids);
        Assert.DoesNotContain(pasado.Id, ids);
    }

    [Fact]
    public async Task El_detalle_de_un_directivo_o_del_presidente_no_trae_entrenamientos()
    {
        var c = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Actividades.CrearEntrenamientoAsync([c.C2014], EnDias(3));

        var directivo = await LeerAsync(c.P.Cliente, Ficha(c, c.D1));
        var presidente = await LeerAsync(c.P.Cliente, Ficha(c, c.P));

        Assert.Equal("DIRECTIVO", directivo.GetProperty("rol").GetString());
        Assert.Empty(directivo.GetProperty("proximosEntrenamientos").EnumerateArray());
        Assert.Empty(presidente.GetProperty("proximosEntrenamientos").EnumerateArray());
    }

    [Fact]
    public async Task Ningun_otro_rol_ve_la_lista_ni_el_detalle()
    {
        var c = await _fabrica.Actividades.CrearClubAsync();

        foreach (var persona in new[] { c.D1, c.E1, c.Jn(1) })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await persona.Cliente.GetAsync(Lista(c))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await persona.Cliente.GetAsync(Ficha(c, c.E2))).StatusCode);
        }
    }

    [Fact]
    public async Task Un_jugador_o_una_persona_de_otro_club_responde_404()
    {
        var c = await _fabrica.Actividades.CrearClubAsync();

        await NoEncontradoAsync(await c.P.Cliente.GetAsync(Ficha(c, c.Jn(1))));
        await NoEncontradoAsync(await c.P.Cliente.GetAsync(Ficha(c, c.PB)));
        await NoEncontradoAsync(await c.P.Cliente.GetAsync(c.Ruta($"/cuerpo-tecnico/{Guid.NewGuid()}")));
        // El presidente de otro club no ve este cuerpo técnico.
        await NoEncontradoAsync(await c.PB.Cliente.GetAsync(Lista(c)));
    }
}
