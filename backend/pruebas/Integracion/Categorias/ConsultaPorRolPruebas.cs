using System.Net;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// Cada quien consulta las categorías que le corresponden: el DIRECTIVO todas, el ENTRENADOR solo
/// las activas que tiene asignadas y el JUGADOR ninguna por estos endpoints (constitución §7.5 y
/// §20; RF-031 a RF-034; historia 7, escenarios 1 a 4, 7 y 9; CE-007).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConsultaPorRolPruebas
{
    private readonly FabricaApi _fabrica;

    public ConsultaPorRolPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_directivo_ve_todas_las_categorias_con_su_contenido_y_las_dos_listas()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var activa = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var inactiva = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false);
        var equipo = await e.Sembrar.CrearEquipoAsync(activa, "A");
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, activa);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);
        await e.Sembrar.AsignarEntrenadorAsync(activa, e.IntegranteEntrenador);
        var (_, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);

        var lista = await e.Directivo.ListaAsync(e.Categorias);
        var detalle = await (await e.Directivo.GetAsync(e.RutaCategoria(activa.Id))).JsonAsync();

        Assert.Equal([activa.Id, inactiva.Id], lista.Ids("categoriaId"));
        Assert.Equal(1, lista[0].GetProperty("numeroJugadores").GetInt32());
        Assert.Single(lista[0].Lista("equipos"));
        Assert.Single(lista[0].Lista("entrenadores"));
        Assert.Equal([jugador.Id], detalle.Lista("jugadores").Ids("usuarioRolId"));
        Assert.Equal(HttpStatusCode.OK, (await e.Directivo.GetAsync(e.RutaCategoria(inactiva.Id))).StatusCode);
        Assert.Equal([e.IntegranteJugador.Id], (await e.Directivo.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
        Assert.Equal([retirado.Id], (await e.Directivo.ListaAsync(e.Retirados)).Ids("usuarioRolId"));
    }

    [Fact]
    public async Task El_entrenador_ve_solo_sus_categorias_con_todos_sus_jugadores_dirija_o_no_su_equipo()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var suya = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var ajena = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        var equipoA = await e.Sembrar.CrearEquipoAsync(suya, "A");
        var equipoB = await e.Sembrar.CrearEquipoAsync(suya, "B");
        var asignacion = await e.Sembrar.AsignarEntrenadorAsync(suya, e.IntegranteEntrenador);
        await e.Sembrar.DirigirEquipoAsync(asignacion, equipoA);
        var (_, delA) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, suya, apellidos: "Arias");
        var (_, delB) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, suya, apellidos: "Bedoya");
        var (_, sinEquipo) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, suya, apellidos: "Castro");
        var (_, deLaAjena) = await e.Sembrar.CrearJugadorAsync(e.Club, 2016, ajena);
        await e.Sembrar.PonerEnEquipoAsync(delA, equipoA);
        await e.Sembrar.PonerEnEquipoAsync(delB, equipoB);

        var lista = await e.Entrenador.ListaAsync(e.Categorias);
        var respuesta = await e.Entrenador.GetAsync(e.RutaCategoria(suya.Id));
        var detalle = await respuesta.JsonAsync();

        Assert.Equal([suya.Id], lista.Ids("categoriaId"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(2, detalle.Lista("equipos").Count);

        // Ve a los tres, también al del equipo B, que no dirige, y al que no tiene equipo (RF-029).
        var jugadores = detalle.Lista("jugadores");
        Assert.Equal([delA.Id, delB.Id, sinEquipo.Id], jugadores.Ids("usuarioRolId"));
        Assert.Equal("Bedoya", jugadores[1].GetProperty("apellidos").GetString());
        Assert.Equal(2014, jugadores[1].GetProperty("anioNacimiento").GetInt32());
        Assert.Equal("B", jugadores[1].Lista("equipos").Single().GetProperty("nombre").GetString());

        // La que no tiene asignada no existe para él, ni con su identificador real, ni sus jugadores.
        var deLaAjenaRespuesta = await e.Entrenador.GetAsync(e.RutaCategoria(ajena.Id));
        Assert.Equal(HttpStatusCode.NotFound, deLaAjenaRespuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(deLaAjenaRespuesta));
        Assert.DoesNotContain(deLaAjena.Apellidos, await deLaAjenaRespuesta.Content.ReadAsStringAsync());
        Assert.DoesNotContain(deLaAjena.Apellidos, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_entrenador_no_ve_sin_categoria_ni_retirados_ni_una_categoria_inactiva_y_sin_asignaciones_no_ve_ninguna()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var inactiva = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false);

        Assert.Empty(await e.Entrenador.ListaAsync(e.Categorias));

        // Una asignación que quedó activa sobre una categoría inactiva tampoco la deja ver.
        await e.Sembrar.AsignarEntrenadorAsync(inactiva, e.IntegranteEntrenador);
        Assert.Empty(await e.Entrenador.ListaAsync(e.Categorias));
        Assert.Equal(HttpStatusCode.NotFound, (await e.Entrenador.GetAsync(e.RutaCategoria(inactiva.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Entrenador.GetAsync(e.RutaCategoria(Guid.NewGuid()))).StatusCode);

        foreach (var ruta in new[] { e.SinCategoria, e.Retirados })
        {
            var respuesta = await e.Entrenador.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(e.IntegranteJugador.Apellidos, await respuesta.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Al_retirarle_la_categoria_deja_de_verla_en_su_siguiente_accion_con_la_misma_sesion()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);
        await e.Presidente.PutAsync(e.EntrenadorDe(categoriaId, e.IntegranteEntrenador.Id), new { });
        Assert.Equal(HttpStatusCode.OK, (await e.Entrenador.GetAsync(e.RutaCategoria(categoriaId))).StatusCode);

        await e.Presidente.DeleteAsync(e.EntrenadorDe(categoriaId, e.IntegranteEntrenador.Id));

        Assert.Empty(await e.Entrenador.ListaAsync(e.Categorias));
        Assert.Equal(HttpStatusCode.NotFound, (await e.Entrenador.GetAsync(e.RutaCategoria(categoriaId))).StatusCode);
    }

    [Fact]
    public async Task Un_jugador_no_consulta_categorias_ni_listas_y_nadie_de_otro_club_ve_una_categoria_de_este()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, EscenarioCategorias.AnioDelJugador);
        var (_, companero) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria, apellidos: "Quintero");

        // Ni siquiera la suya: lo suyo lo ve por "Mi categoría", sin sus compañeros.
        foreach (var ruta in new[] { e.Categorias, e.RutaCategoria(categoria.Id), e.SinCategoria, e.Retirados, e.Candidatos(categoria.Id) })
        {
            var respuesta = await e.Jugador.GetAsync(ruta);
            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{ruta}: {(int)respuesta.StatusCode}");
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(companero.Apellidos, await respuesta.Content.ReadAsStringAsync());
        }

        var enSuClub = $"/api/clubes/{e.OtroClub.Id}/categorias/{categoria.Id}";
        var delOtroClub = await e.PresidenteDeOtroClub.GetAsync(enSuClub);
        Assert.Equal(HttpStatusCode.NotFound, delOtroClub.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.PresidenteDeOtroClub.GetAsync(e.RutaCategoria(categoria.Id))).StatusCode);
        Assert.Empty(await e.PresidenteDeOtroClub.ListaAsync(EscenarioCategorias.RutaCategorias(e.OtroClub)));
    }
}
