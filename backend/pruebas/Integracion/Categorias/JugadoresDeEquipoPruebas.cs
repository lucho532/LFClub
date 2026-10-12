using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// Solo el PRESIDENTE decide en qué equipos juega cada jugador, y un jugador nunca está en un
/// equipo de otra categoría (constitución §11.3 y §20; RF-025 a RF-027; historia 5, escenarios 3
/// a 6).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class JugadoresDeEquipoPruebas
{
    private readonly FabricaApi _fabrica;

    public JugadoresDeEquipoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_jugador_puede_estar_en_varios_equipos_y_cuenta_una_vez_en_la_categoria()
    {
        var (e, categoria, equipoA, equipoB) = await ConDosEquiposAsync();
        var (_, enLosDos) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria, apellidos: "Arias");
        var (_, sinEquipo) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria, apellidos: "Zuluaga");

        await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoA.Id, enLosDos.Id), new { });
        var respuesta = await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoB.Id, enLosDos.Id), new { });
        var repetida = await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoB.Id, enLosDos.Id), new { });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        var detalle = await repetida.JsonAsync();
        Assert.Equal(2, detalle.GetProperty("numeroJugadores").GetInt32());
        Assert.All(detalle.Lista("equipos"), equipo => Assert.Equal(1, equipo.GetProperty("numeroJugadores").GetInt32()));
        var jugadores = detalle.Lista("jugadores");
        Assert.Equal(["A", "B"], jugadores[0].Lista("equipos").Select(equipo => equipo.GetProperty("nombre").GetString()));

        // Estar en un equipo es opcional (RF-026): sigue en la categoría, sin equipo.
        Assert.Equal(sinEquipo.Id, jugadores[1].GetProperty("usuarioRolId").GetGuid());
        Assert.Empty(jugadores[1].Lista("equipos"));
        Assert.Equal(2, (await e.IntegranteGuardadoAsync(enLosDos.Id))!.Equipos.Count);
    }

    [Fact]
    public async Task Sacarlo_de_un_equipo_lo_deja_en_el_otro_y_el_equipo_ya_no_se_puede_borrar()
    {
        var (e, categoria, equipoA, equipoB) = await ConDosEquiposAsync();
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria);
        await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoA.Id, jugador.Id), new { });
        await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoB.Id, jugador.Id), new { });

        var respuesta = await e.Presidente.DeleteAsync(e.JugadorDeEquipo(categoria.Id, equipoA.Id, jugador.Id));
        var otraVez = await e.Presidente.DeleteAsync(e.JugadorDeEquipo(categoria.Id, equipoA.Id, jugador.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otraVez.StatusCode);
        var detalle = await otraVez.JsonAsync();
        Assert.Equal([equipoB.Id], detalle.Lista("jugadores").Single().Lista("equipos").Ids("equipoId"));
        Assert.Equal(categoria.Id, (await e.IntegranteGuardadoAsync(jugador.Id))!.CategoriaId);

        // El equipo A quedó vacío, pero tuvo un jugador: solo se desactiva (RF-024a).
        var equipoVacio = detalle.Lista("equipos").Single(equipo => equipo.GetProperty("equipoId").GetGuid() == equipoA.Id);
        Assert.Equal(0, equipoVacio.GetProperty("numeroJugadores").GetInt32());
        Assert.False(equipoVacio.GetProperty("sePuedeBorrar").GetBoolean());
    }

    [Fact]
    public async Task Un_jugador_no_entra_en_un_equipo_de_una_categoria_que_no_es_la_suya()
    {
        var (e, categoria, equipoA, _) = await ConDosEquiposAsync();
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        var (_, deOtra) = await e.Sembrar.CrearJugadorAsync(e.Club, 2016, otra);
        var (_, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);

        // De otra categoría, sin categoría, retirado y un entrenador: ninguno es jugador de esta categoría.
        foreach (var ajeno in new[] { deOtra.Id, e.IntegranteJugador.Id, retirado.Id, e.IntegranteEntrenador.Id })
        {
            var respuesta = await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoA.Id, ajeno), new { });

            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal("jugador_de_otra_categoria", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        var inexistente = await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoA.Id, Guid.NewGuid()), new { });
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Empty((await e.IntegranteGuardadoAsync(deOtra.Id))!.Equipos);
        Assert.True((await e.DetalleAsync(categoria.Id)).Lista("equipos").All(equipo => equipo.GetProperty("sePuedeBorrar").GetBoolean()));
    }

    [Fact]
    public async Task Al_cambiar_de_categoria_sale_de_todos_sus_equipos_y_al_volver_no_los_recupera()
    {
        var (e, categoria, equipoA, equipoB) = await ConDosEquiposAsync();
        var otraId = await e.CrearCategoriaPorApiAsync(2016);
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria);
        await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoA.Id, jugador.Id), new { });
        await e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoB.Id, jugador.Id), new { });

        var enLaOtra = await (await e.Presidente.PutAsync(e.CategoriaDe(jugador.Id), new { categoriaId = otraId })).JsonAsync();
        var deVuelta = await (await e.Presidente.PutAsync(e.CategoriaDe(jugador.Id), new { categoriaId = categoria.Id })).JsonAsync();

        Assert.Empty(enLaOtra.Lista("jugadores").Single().Lista("equipos"));
        Assert.Empty(deVuelta.Lista("jugadores").Single().Lista("equipos"));
        Assert.All(deVuelta.Lista("equipos"), equipo => Assert.Equal(0, equipo.GetProperty("numeroJugadores").GetInt32()));
    }

    [Fact]
    public async Task Ponerlo_en_un_equipo_y_cambiarlo_de_categoria_a_la_vez_nunca_lo_deja_en_un_equipo_ajeno()
    {
        var (e, categoria, equipoA, _) = await ConDosEquiposAsync();
        var otraId = await e.CrearCategoriaPorApiAsync(2016);

        for (var vuelta = 0; vuelta < 5; vuelta++)
        {
            var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria);

            await Task.WhenAll(
                e.Presidente.PutAsync(e.JugadorDeEquipo(categoria.Id, equipoA.Id, jugador.Id), new { }),
                e.Presidente.PutAsync(e.CategoriaDe(jugador.Id), new { categoriaId = otraId }));

            // Gane quien gane, termina en la otra categoría y sin el equipo de la primera.
            var guardado = (await e.IntegranteGuardadoAsync(jugador.Id))!;
            Assert.Equal(otraId, guardado.CategoriaId);
            Assert.Empty(guardado.Equipos);
        }
    }

    [Fact]
    public async Task Ni_el_entrenador_que_dirige_el_equipo_ni_nadie_de_otro_club_pone_o_saca_jugadores()
    {
        var (e, categoria, equipoA, _) = await ConDosEquiposAsync();
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipoA);
        var asignacion = await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegranteEntrenador);
        await e.Sembrar.DirigirEquipoAsync(asignacion, equipoA);
        var ruta = e.JugadorDeEquipo(categoria.Id, equipoA.Id, jugador.Id);

        foreach (var (_, cliente) in e.QuienesNoGestionan)
        {
            await AislamientoCategoriasPruebas.ComprobarAsync(
                cliente, HttpStatusCode.Forbidden, "rol_no_autorizado", ("PUT", ruta, null), ("DELETE", ruta, null));
        }

        var enSuClub = $"/api/clubes/{e.OtroClub.Id}/categorias/{categoria.Id}/equipos/{equipoA.Id}/jugadores/{jugador.Id}";
        await AislamientoCategoriasPruebas.ComprobarAsync(
            e.PresidenteDeOtroClub, HttpStatusCode.NotFound, "no_encontrado", ("PUT", enSuClub, null), ("DELETE", enSuClub, null));

        Assert.Single((await e.IntegranteGuardadoAsync(jugador.Id))!.Equipos);
    }

    private async Task<(EscenarioCategorias E, Categoria Categoria, Equipo A, Equipo B)> ConDosEquiposAsync()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        return (e, categoria, await e.Sembrar.CrearEquipoAsync(categoria, "A"), await e.Sembrar.CrearEquipoAsync(categoria, "B"));
    }
}
