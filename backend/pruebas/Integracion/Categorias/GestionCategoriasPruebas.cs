using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// El PRESIDENTE crea, desactiva, reactiva y borra las categorías de su club (constitución §11 y
/// §14; RF-001 a RF-007; historia 1).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class GestionCategoriasPruebas
{
    private readonly FabricaApi _fabrica;

    public GestionCategoriasPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Crear_deja_la_categoria_activa_vacia_y_la_lista_sale_por_anio_con_las_inactivas()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false);

        var respuesta = await e.Presidente.PostAsync(e.Categorias, new { anio = 2015 });
        await e.Presidente.PostAsync(e.Categorias, new { anio = 2012 });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creada = await respuesta.JsonAsync();
        var categoria = creada.GetProperty("categoria");
        Assert.Equal(0, creada.GetProperty("jugadoresUbicados").GetInt32());
        Assert.Equal(2015, categoria.GetProperty("anio").GetInt32());
        Assert.True(categoria.GetProperty("activa").GetBoolean());
        Assert.True(categoria.GetProperty("sePuedeBorrar").GetBoolean());
        Assert.Equal(0, categoria.GetProperty("numeroJugadores").GetInt32());
        Assert.Empty(categoria.Lista("equipos"));
        Assert.Empty(categoria.Lista("entrenadores"));

        var lista = await e.Presidente.ListaAsync(e.Categorias);
        Assert.Equal([2012, 2015, 2016], lista.Select(fila => fila.GetProperty("anio").GetInt32()));
        Assert.Equal([true, true, false], lista.Select(fila => fila.GetProperty("activa").GetBoolean()));
    }

    [Fact]
    public async Task Un_anio_vacio_que_no_es_un_anio_o_posterior_al_actual_responde_400_y_no_crea_nada()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var cuerpos = new object[]
        {
            new { }, new { anio = (int?)null }, new { anio = "abcd" }, new { anio = 201 }, new { anio = 0 },
            new { anio = DateTime.UtcNow.Year + 1 },
        };

        foreach (var cuerpo in cuerpos)
        {
            var respuesta = await e.Presidente.PostAsync(e.Categorias, cuerpo);

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.True((await respuesta.JsonAsync()).GetProperty("errores").TryGetProperty("anio", out _));
        }

        Assert.Empty(await e.Presidente.ListaAsync(e.Categorias));
    }

    [Fact]
    public async Task Repetir_el_anio_responde_409_y_dice_si_la_que_existe_esta_inactiva()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.CrearCategoriaPorApiAsync(2015);
        await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false);

        var activa = await e.Presidente.PostAsync(e.Categorias, new { anio = 2015 });
        var inactiva = await e.Presidente.PostAsync(e.Categorias, new { anio = 2016 });

        Assert.Equal(HttpStatusCode.Conflict, activa.StatusCode);
        Assert.Equal("categoria_ya_existe", await ClienteDePrueba.CodigoAsync(activa));
        Assert.Equal(HttpStatusCode.Conflict, inactiva.StatusCode);
        Assert.Equal("categoria_inactiva_ya_existe", await ClienteDePrueba.CodigoAsync(inactiva));
        Assert.Contains("reactivar", await ClienteDePrueba.TituloAsync(inactiva), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, (await e.Presidente.ListaAsync(e.Categorias)).Count);
    }

    [Fact]
    public async Task Dos_altas_simultaneas_del_mismo_anio_crean_una_sola()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var otroPresidente = await _fabrica.CrearClienteDePrueba()
            .ConSesionDeAsync((await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.PRESIDENTE)).Usuario);

        var respuestas = await Task.WhenAll(
            e.Presidente.PostAsync(e.Categorias, new { anio = 2013 }),
            otroPresidente.PostAsync(e.Categorias, new { anio = 2013 }));

        Assert.Equal(
            [HttpStatusCode.Created, HttpStatusCode.Conflict], respuestas.Select(respuesta => respuesta.StatusCode).Order());
        Assert.Equal("categoria_ya_existe", await ClienteDePrueba.CodigoAsync(respuestas.Single(r => !r.IsSuccessStatusCode)));
        Assert.Single(await e.Presidente.ListaAsync(e.Categorias));
    }

    [Fact]
    public async Task Crear_recoge_a_los_jugadores_sin_categoria_de_ese_anio_y_ya_no_se_puede_borrar()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.Sembrar.CrearJugadorAsync(e.Club, EscenarioCategorias.AnioDelJugador);
        await e.Sembrar.CrearJugadorAsync(e.Club, 2016);

        var creada = await (await e.Presidente.PostAsync(e.Categorias, new { anio = EscenarioCategorias.AnioDelJugador })).JsonAsync();
        var categoria = creada.GetProperty("categoria");

        Assert.Equal(2, creada.GetProperty("jugadoresUbicados").GetInt32());
        Assert.Equal(2, categoria.GetProperty("numeroJugadores").GetInt32());
        Assert.False(categoria.GetProperty("sePuedeBorrar").GetBoolean());

        // Caso límite de la spec: si al crearla entraron jugadores, ya solo se puede desactivar.
        var borrado = await e.Presidente.DeleteAsync(e.RutaCategoria(categoria.GetProperty("categoriaId").GetGuid()));
        Assert.Equal(HttpStatusCode.Conflict, borrado.StatusCode);
        Assert.Equal("categoria_con_historial", await ClienteDePrueba.CodigoAsync(borrado));
    }

    [Fact]
    public async Task Dos_clubes_crean_el_mismo_anio_y_ninguno_ve_la_del_otro()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var deEste = await e.CrearCategoriaPorApiAsync(2015);

        var enElOtro = await e.PresidenteDeOtroClub.PostAsync(EscenarioCategorias.RutaCategorias(e.OtroClub), new { anio = 2015 });

        Assert.Equal(HttpStatusCode.Created, enElOtro.StatusCode);
        var delOtro = await e.PresidenteDeOtroClub.ListaAsync(EscenarioCategorias.RutaCategorias(e.OtroClub));
        Assert.DoesNotContain(deEste, delOtro.Ids("categoriaId"));
        Assert.Equal([deEste], (await e.Presidente.ListaAsync(e.Categorias)).Ids("categoriaId"));
    }

    [Fact]
    public async Task El_directivo_ve_la_lista_y_no_crea_y_los_demas_no_hacen_ninguna_de_las_dos_cosas()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.CrearCategoriaPorApiAsync(2015);

        Assert.Single(await e.Directivo.ListaAsync(e.Categorias));
        foreach (var (rol, cliente) in e.QuienesNoGestionan)
        {
            var alCrear = await cliente.PostAsync(e.Categorias, new { anio = 2016 });
            Assert.True(alCrear.StatusCode == HttpStatusCode.Forbidden, $"{rol} al crear: {(int)alCrear.StatusCode}");
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(alCrear));
        }

        var delJugador = await e.Jugador.GetAsync(e.Categorias);
        Assert.Equal(HttpStatusCode.Forbidden, delJugador.StatusCode);
        Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(delJugador));
        Assert.DoesNotContain("2015", await delJugador.Content.ReadAsStringAsync());

        var sinSesion = _fabrica.CrearClienteDePrueba();
        Assert.Equal(HttpStatusCode.Unauthorized, (await sinSesion.GetAsync(e.Categorias)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await sinSesion.PostAsync(e.Categorias, new { anio = 2016 })).StatusCode);
        Assert.Single(await e.Presidente.ListaAsync(e.Categorias));
    }
}
