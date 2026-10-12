using System.Net;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// El PRESIDENTE crea, renombra, desactiva y borra los equipos de una categoría (constitución
/// §11.3 y §14; RF-022 a RF-024a y RF-030; historia 5, escenarios 1, 2, 9, 10 y 12).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EquiposPruebas
{
    private readonly FabricaApi _fabrica;

    public EquiposPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Crear_deja_el_equipo_vacio_dentro_de_la_categoria()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);

        await e.Presidente.PostAsync(e.Equipos(categoriaId), new { nombre = "B" });
        var respuesta = await e.Presidente.PostAsync(e.Equipos(categoriaId), new { nombre = "  A  " });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var equipos = (await respuesta.JsonAsync()).Lista("equipos");
        Assert.Equal(["A", "B"], equipos.Select(equipo => equipo.GetProperty("nombre").GetString()));
        Assert.All(equipos, equipo =>
        {
            Assert.Equal(0, equipo.GetProperty("numeroJugadores").GetInt32());
            Assert.True(equipo.GetProperty("sePuedeBorrar").GetBoolean());
        });

        // Tener equipos no es haber tenido jugadores ni entrenadores: la categoría se sigue pudiendo borrar.
        Assert.True((await e.DetalleAsync(categoriaId)).GetProperty("sePuedeBorrar").GetBoolean());
    }

    [Fact]
    public async Task El_nombre_no_se_repite_en_la_categoria_sin_distinguir_mayusculas_y_si_en_otra()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);
        var otraId = await e.CrearCategoriaPorApiAsync(2016);
        await e.Presidente.PostAsync(e.Equipos(categoriaId), new { nombre = "Élite" });

        foreach (var repetido in new[] { "Élite", "élite", " ÉLITE " })
        {
            var respuesta = await e.Presidente.PostAsync(e.Equipos(categoriaId), new { nombre = repetido });
            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal("equipo_ya_existe", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Equal(HttpStatusCode.Created, (await e.Presidente.PostAsync(e.Equipos(otraId), new { nombre = "Élite" })).StatusCode);
        Assert.Single((await e.DetalleAsync(categoriaId)).Lista("equipos"));
    }

    [Fact]
    public async Task Un_nombre_vacio_o_de_mas_de_30_caracteres_responde_400_y_una_categoria_inactiva_409()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);
        var inactiva = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false);

        foreach (var cuerpo in new object[] { new { }, new { nombre = "" }, new { nombre = "   " }, new { nombre = new string('a', 31) } })
        {
            var respuesta = await e.Presidente.PostAsync(e.Equipos(categoriaId), cuerpo);
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.True((await respuesta.JsonAsync()).GetProperty("errores").TryGetProperty("nombre", out _));
        }

        var enInactiva = await e.Presidente.PostAsync(e.Equipos(inactiva.Id), new { nombre = "A" });
        Assert.Equal(HttpStatusCode.Conflict, enInactiva.StatusCode);
        Assert.Equal("categoria_inactiva", await ClienteDePrueba.CodigoAsync(enInactiva));
        Assert.Empty((await e.DetalleAsync(categoriaId)).Lista("equipos"));
    }

    [Fact]
    public async Task Renombrar_conserva_a_sus_jugadores_y_entrenadores_y_no_admite_un_nombre_ya_usado()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, EscenarioCategorias.AnioDelJugador);
        var equipo = await e.Sembrar.CrearEquipoAsync(categoria, "B");
        await e.Sembrar.CrearEquipoAsync(categoria, "A");
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);
        var asignacion = await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegranteEntrenador);
        await e.Sembrar.DirigirEquipoAsync(asignacion, equipo);

        var respuesta = await e.Presidente.PutAsync(e.RutaEquipo(categoria.Id, equipo.Id), new { nombre = "Élite" });
        var mismoNombre = await e.Presidente.PutAsync(e.RutaEquipo(categoria.Id, equipo.Id), new { nombre = "élite" });
        var repetido = await e.Presidente.PutAsync(e.RutaEquipo(categoria.Id, equipo.Id), new { nombre = "a" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var detalle = await respuesta.JsonAsync();
        var renombrado = detalle.Lista("equipos").Single(fila => fila.GetProperty("equipoId").GetGuid() == equipo.Id);
        Assert.Equal("Élite", renombrado.GetProperty("nombre").GetString());
        Assert.Equal(1, renombrado.GetProperty("numeroJugadores").GetInt32());
        Assert.Equal("Élite", detalle.Lista("jugadores").Single().Lista("equipos").Single().GetProperty("nombre").GetString());
        Assert.Equal("Élite", detalle.Lista("entrenadores").Single().Lista("equipos").Single().GetProperty("nombre").GetString());

        // Volver a ponerle su propio nombre, con otras mayúsculas, no choca consigo mismo.
        Assert.Equal(HttpStatusCode.OK, mismoNombre.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal("equipo_ya_existe", await ClienteDePrueba.CodigoAsync(repetido));
    }

    [Fact]
    public async Task Desactivar_lo_quita_de_las_respuestas_deja_a_su_gente_en_la_categoria_y_libera_su_nombre()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, EscenarioCategorias.AnioDelJugador);
        var equipo = await e.Sembrar.CrearEquipoAsync(categoria, "B");
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2014, categoria);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);
        var asignacion = await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegranteEntrenador);
        await e.Sembrar.DirigirEquipoAsync(asignacion, equipo);

        var respuesta = await e.Presidente.PostAsync(e.DesactivacionDeEquipo(categoria.Id, equipo.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var detalle = await respuesta.JsonAsync();
        Assert.Empty(detalle.Lista("equipos"));
        Assert.Empty(detalle.Lista("jugadores").Single().Lista("equipos"));

        // El único equipo que dirigía ya no existe: queda como entrenador de la categoría en general.
        Assert.Empty(detalle.Lista("entrenadores").Single().Lista("equipos"));
        Assert.False(await _fabrica.ConContextoAsync(contexto => contexto.Equipos
            .IgnoreQueryFilters().Where(fila => fila.Id == equipo.Id).Select(fila => fila.Activo).SingleAsync()));

        // Un equipo desactivado no existe para la API, y su nombre queda libre (supuesto 3).
        Assert.Equal(HttpStatusCode.NotFound, (await e.Presidente.PostAsync(e.DesactivacionDeEquipo(categoria.Id, equipo.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Presidente.PutAsync(e.RutaEquipo(categoria.Id, equipo.Id), new { nombre = "C" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await e.Presidente.PostAsync(e.Equipos(categoria.Id), new { nombre = "b" })).StatusCode);
    }

    [Fact]
    public async Task Solo_se_borra_un_equipo_que_nunca_tuvo_jugadores_ni_entrenadores()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var sinUsar = await e.Sembrar.CrearEquipoAsync(categoria, "C");
        var usado = await e.Sembrar.CrearEquipoAsync(categoria, "A", usado: true);

        var borrado = await e.Presidente.DeleteAsync(e.RutaEquipo(categoria.Id, sinUsar.Id));
        var negado = await e.Presidente.DeleteAsync(e.RutaEquipo(categoria.Id, usado.Id));

        Assert.Equal(HttpStatusCode.OK, borrado.StatusCode);
        Assert.Equal([usado.Id], (await borrado.JsonAsync()).Lista("equipos").Ids("equipoId"));
        Assert.Equal(HttpStatusCode.Conflict, negado.StatusCode);
        Assert.Equal("equipo_con_historial", await ClienteDePrueba.CodigoAsync(negado));
        Assert.Contains("desactivar", await ClienteDePrueba.TituloAsync(negado), StringComparison.OrdinalIgnoreCase);
        Assert.False((await e.DetalleAsync(categoria.Id)).Lista("equipos").Single().GetProperty("sePuedeBorrar").GetBoolean());
    }

    [Fact]
    public async Task Solo_el_presidente_de_ese_club_gestiona_los_equipos_y_un_equipo_de_otra_categoria_no_existe()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        var equipo = await e.Sembrar.CrearEquipoAsync(categoria, "A");
        var nombre = "{\"nombre\":\"Z\"}";
        var enSuClub = $"/api/clubes/{e.OtroClub.Id}/categorias/{categoria.Id}/equipos";

        foreach (var (_, cliente) in e.QuienesNoGestionan)
        {
            await AislamientoCategoriasPruebas.ComprobarAsync(cliente, HttpStatusCode.Forbidden, "rol_no_autorizado",
                ("POST", e.Equipos(categoria.Id), nombre),
                ("PUT", e.RutaEquipo(categoria.Id, equipo.Id), nombre),
                ("POST", e.DesactivacionDeEquipo(categoria.Id, equipo.Id), null),
                ("DELETE", e.RutaEquipo(categoria.Id, equipo.Id), null));
        }

        await AislamientoCategoriasPruebas.ComprobarAsync(e.PresidenteDeOtroClub, HttpStatusCode.NotFound, "no_encontrado",
            ("POST", enSuClub, nombre),
            ("PUT", $"{enSuClub}/{equipo.Id}", nombre),
            ("POST", $"{enSuClub}/{equipo.Id}/desactivacion", null),
            ("DELETE", $"{enSuClub}/{equipo.Id}", null));

        // El equipo existe, pero no en la categoría de la ruta.
        await AislamientoCategoriasPruebas.ComprobarAsync(e.Presidente, HttpStatusCode.NotFound, "no_encontrado",
            ("PUT", e.RutaEquipo(otra.Id, equipo.Id), nombre),
            ("POST", e.DesactivacionDeEquipo(otra.Id, equipo.Id), null),
            ("DELETE", e.RutaEquipo(otra.Id, equipo.Id), null));

        var guardado = Assert.Single((await e.DetalleAsync(categoria.Id)).Lista("equipos"));
        Assert.Equal("A", guardado.GetProperty("nombre").GetString());
    }
}
