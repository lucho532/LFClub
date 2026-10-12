using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// El PRESIDENTE ubica a un jugador sin categoría o lo pasa a otra, y un jugador nunca está en dos
/// categorías (constitución §11.2 y §20; RF-013 a RF-016; historia 4; CE-009).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CambioDeCategoriaPruebas
{
    private readonly FabricaApi _fabrica;

    public CambioDeCategoriaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Ubicar_a_un_jugador_sin_categoria_lo_saca_de_la_lista_y_lo_pone_en_ella_senalado_si_no_es_su_anio()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2016);

        var respuesta = await e.Presidente.PutAsync(e.CategoriaDe(e.IntegranteJugador.Id), new { categoriaId });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var detalle = await respuesta.JsonAsync();
        Assert.Equal(categoriaId, detalle.GetProperty("categoriaId").GetGuid());
        var jugador = Assert.Single(detalle.Lista("jugadores"));
        Assert.Equal(e.IntegranteJugador.Id, jugador.GetProperty("usuarioRolId").GetGuid());
        Assert.True(jugador.GetProperty("fueraDeSuAnio").GetBoolean());
        Assert.Equal(EscenarioCategorias.AnioDelJugador, jugador.GetProperty("anioNacimiento").GetInt32());
        Assert.False(detalle.GetProperty("sePuedeBorrar").GetBoolean());
        Assert.Empty(await e.Presidente.ListaAsync(e.SinCategoria));

        // Crear después la categoría de su año no lo mueve (RF-011).
        var deSuAnio = await (await e.Presidente.PostAsync(e.Categorias, new { anio = EscenarioCategorias.AnioDelJugador })).JsonAsync();
        Assert.Equal(0, deSuAnio.GetProperty("jugadoresUbicados").GetInt32());
        Assert.Equal(categoriaId, (await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!.CategoriaId);
    }

    [Fact]
    public async Task Pasarlo_a_otra_lo_deja_solo_en_la_nueva_sin_sus_equipos_y_con_los_mismos_datos()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var origen = await e.Sembrar.CrearCategoriaAsync(e.Club, 2014);
        var destino = await e.Sembrar.CrearCategoriaAsync(e.Club, 2013);
        var equipo = await e.Sembrar.CrearEquipoAsync(origen, "A");
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, origen);
        await e.Sembrar.CrearJugadorAsync(e.Club, 2014, origen);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);

        var respuesta = await e.Presidente.PutAsync(e.CategoriaDe(jugador.Id), new { categoriaId = destino.Id });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal([jugador.Id], (await respuesta.JsonAsync()).Lista("jugadores").Ids("usuarioRolId"));
        var enOrigen = await e.DetalleAsync(origen.Id);
        Assert.Equal(1, enOrigen.GetProperty("numeroJugadores").GetInt32());
        Assert.DoesNotContain(jugador.Id, enOrigen.Lista("jugadores").Ids("usuarioRolId"));
        Assert.Equal(0, enOrigen.Lista("equipos").Single().GetProperty("numeroJugadores").GetInt32());

        // RF-015: es el mismo jugador, con los mismos datos; RF-027: sale de los equipos de la anterior.
        var guardado = (await e.IntegranteGuardadoAsync(jugador.Id))!;
        Assert.Equal(destino.Id, guardado.CategoriaId);
        Assert.Empty(guardado.Equipos);
        Assert.Equal(jugador.NumeroDocumento, guardado.NumeroDocumento);
        Assert.Equal(jugador.FechaNacimiento, guardado.FechaNacimiento);
        Assert.Equal(jugador.UsuarioId, guardado.UsuarioId);
        Assert.Equal(Rol.JUGADOR, guardado.Rol);
    }

    [Fact]
    public async Task Ubicarlo_en_la_categoria_en_la_que_ya_esta_no_cambia_nada_y_conserva_sus_equipos()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2014);
        var equipo = await e.Sembrar.CrearEquipoAsync(categoria, "A");
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);

        var respuesta = await e.Presidente.PutAsync(e.CategoriaDe(jugador.Id), new { categoriaId = categoria.Id });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Single((await e.IntegranteGuardadoAsync(jugador.Id))!.Equipos);
    }

    [Fact]
    public async Task No_se_ubica_en_una_categoria_inactiva_inexistente_o_de_otro_club_ni_sin_decir_cual()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var inactiva = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015, activa: false);
        var deOtroClub = await e.Sembrar.CrearCategoriaAsync(e.OtroClub, 2014);
        var ruta = e.CategoriaDe(e.IntegranteJugador.Id);

        var enInactiva = await e.Presidente.PutAsync(ruta, new { categoriaId = inactiva.Id });
        var enAjena = await e.Presidente.PutAsync(ruta, new { categoriaId = deOtroClub.Id });
        var enInexistente = await e.Presidente.PutAsync(ruta, new { categoriaId = Guid.NewGuid() });
        var sinCategoria = await e.Presidente.PutAsync(ruta, new { });
        var jugadorInexistente = await e.Presidente.PutAsync(e.CategoriaDe(Guid.NewGuid()), new { categoriaId = inactiva.Id });

        Assert.Equal("categoria_inactiva", await ClienteDePrueba.CodigoAsync(enInactiva));
        Assert.Equal(HttpStatusCode.Conflict, enInactiva.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, enAjena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, enInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, jugadorInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sinCategoria.StatusCode);
        Assert.True((await sinCategoria.JsonAsync()).GetProperty("errores").TryGetProperty("categoriaId", out _));
        Assert.Null((await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!.CategoriaId);
    }

    [Fact]
    public async Task Solo_los_jugadores_aprobados_y_activos_tienen_categoria()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);
        var (_, enEspera) = await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 2015);
        var (_, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);
        var noJugadores = new[]
        {
            e.IntegranteEntrenador.Id, e.IntegranteDirectivo.Id, e.IntegrantePresidente.Id, enEspera.Id, retirado.Id,
        };

        foreach (var usuarioRolId in noJugadores)
        {
            var respuesta = await e.Presidente.PutAsync(e.CategoriaDe(usuarioRolId), new { categoriaId });

            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal("no_es_jugador", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.Null((await e.IntegranteGuardadoAsync(usuarioRolId))!.CategoriaId);
        }

        Assert.Empty((await e.DetalleAsync(categoriaId)).Lista("jugadores"));
    }

    [Fact]
    public async Task Dos_cambios_simultaneos_del_mismo_jugador_lo_dejan_en_una_sola_categoria()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var una = await e.Sembrar.CrearCategoriaAsync(e.Club, 2012);
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2013);
        var otroPresidente = await _fabrica.CrearClienteDePrueba()
            .ConSesionDeAsync((await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.PRESIDENTE)).Usuario);
        var ruta = e.CategoriaDe(e.IntegranteJugador.Id);

        var respuestas = await Task.WhenAll(
            e.Presidente.PutAsync(ruta, new { categoriaId = una.Id }),
            otroPresidente.PutAsync(ruta, new { categoriaId = otra.Id }));

        Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode));
        var enUna = (await e.DetalleAsync(una.Id)).Lista("jugadores").Count;
        var enOtra = (await e.DetalleAsync(otra.Id)).Lista("jugadores").Count;
        Assert.Equal(1, enUna + enOtra);

        // CE-009: cada jugador del club está en una categoría o en "Sin categoría", nunca en dos sitios.
        var total = (await e.Presidente.ListaAsync(e.Categorias)).Sum(c => c.GetProperty("numeroJugadores").GetInt32());
        Assert.Equal(1, total + (await e.Presidente.ListaAsync(e.SinCategoria)).Count);
    }

    [Fact]
    public async Task Solo_el_presidente_de_ese_club_ubica_o_cambia_de_categoria()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var cuerpo = $"{{\"categoriaId\":\"{categoria.Id}\"}}";

        foreach (var (_, cliente) in e.QuienesNoGestionan)
        {
            await AislamientoCategoriasPruebas.ComprobarAsync(cliente, HttpStatusCode.Forbidden, "rol_no_autorizado",
                ("PUT", e.CategoriaDe(e.IntegranteJugador.Id), cuerpo));
        }

        // El presidente de otro club, en su club y con los identificadores reales de este.
        await AislamientoCategoriasPruebas.ComprobarAsync(e.PresidenteDeOtroClub, HttpStatusCode.NotFound, "no_encontrado",
            ("PUT", $"/api/clubes/{e.OtroClub.Id}/jugadores/{e.IntegranteJugador.Id}/categoria", cuerpo));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _fabrica.CrearClienteDePrueba().PutAsync(e.CategoriaDe(e.IntegranteJugador.Id), new { categoriaId = categoria.Id })).StatusCode);
        Assert.Null((await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!.CategoriaId);
    }
}
