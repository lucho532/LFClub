using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// El PRESIDENTE indica qué equipos de una categoría dirige cada entrenador asignado a ella
/// (constitución §11.3; RF-028 a RF-030; historia 5, escenarios 7 y 8).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EquiposDeEntrenadorPruebas
{
    private readonly FabricaApi _fabrica;

    public EquiposDeEntrenadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Indicar_los_equipos_que_dirige_reemplaza_la_lista_completa()
    {
        var (e, categoria, equipoA, equipoB) = await ConEntrenadorYDosEquiposAsync();
        var ruta = e.EquiposQueDirige(categoria.Id, e.IntegranteEntrenador.Id);

        var soloA = await e.Presidente.PutAsync(ruta, new { equipoIds = new[] { equipoA.Id } });
        var losDos = await e.Presidente.PutAsync(ruta, new { equipoIds = new[] { equipoB.Id, equipoA.Id, equipoA.Id } });
        var soloB = await e.Presidente.PutAsync(ruta, new { equipoIds = new[] { equipoB.Id } });
        var ninguno = await e.Presidente.PutAsync(ruta, new { equipoIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.OK, soloA.StatusCode);
        Assert.Equal([equipoA.Id], await DirigeAsync(soloA));
        Assert.Equal([equipoA.Id, equipoB.Id], await DirigeAsync(losDos));
        Assert.Equal([equipoB.Id], await DirigeAsync(soloB));

        // Sin ningún equipo es entrenador de la categoría en general, y sigue asignado (escenario 5.8).
        Assert.Empty(await DirigeAsync(ninguno));
        var detalle = await ninguno.JsonAsync();
        Assert.Single(detalle.Lista("entrenadores"));
        Assert.All(detalle.Lista("equipos"), equipo => Assert.False(equipo.GetProperty("sePuedeBorrar").GetBoolean()));
    }

    [Fact]
    public async Task Un_equipo_desactivado_inexistente_o_de_otra_categoria_responde_404_y_no_cambia_nada()
    {
        var (e, categoria, equipoA, equipoB) = await ConEntrenadorYDosEquiposAsync();
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        var deOtra = await e.Sembrar.CrearEquipoAsync(otra, "A");
        var desactivado = await e.Sembrar.CrearEquipoAsync(categoria, "C", activo: false);
        var ruta = e.EquiposQueDirige(categoria.Id, e.IntegranteEntrenador.Id);
        await e.Presidente.PutAsync(ruta, new { equipoIds = new[] { equipoA.Id } });

        foreach (var ajeno in new[] { deOtra.Id, desactivado.Id, Guid.NewGuid() })
        {
            var respuesta = await e.Presidente.PutAsync(ruta, new { equipoIds = new[] { equipoB.Id, ajeno } });

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        var sinLista = await e.Presidente.PutAsync(ruta, new { });
        Assert.Equal(HttpStatusCode.BadRequest, sinLista.StatusCode);
        Assert.True((await sinLista.JsonAsync()).GetProperty("errores").TryGetProperty("equipoIds", out _));
        Assert.Equal([equipoA.Id], Dirige(await e.DetalleAsync(categoria.Id)));
    }

    [Fact]
    public async Task Solo_dirige_un_equipo_quien_esta_asignado_a_la_categoria()
    {
        var (e, categoria, equipoA, _) = await ConEntrenadorYDosEquiposAsync();
        var (_, noAsignado) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Dominio.Enumeraciones.Rol.ENTRENADOR);
        var (_, retiradoDeElla) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Dominio.Enumeraciones.Rol.ENTRENADOR);
        await e.Sembrar.AsignarEntrenadorAsync(categoria, retiradoDeElla, activa: false);

        foreach (var integrante in new[] { noAsignado, retiradoDeElla })
        {
            var respuesta = await e.Presidente.PutAsync(
                e.EquiposQueDirige(categoria.Id, integrante.Id), new { equipoIds = new[] { equipoA.Id } });

            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal("entrenador_no_asignado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.True((await e.DetalleAsync(categoria.Id)).Lista("equipos").All(equipo => equipo.GetProperty("sePuedeBorrar").GetBoolean()));
    }

    [Fact]
    public async Task Al_retirarle_la_categoria_deja_de_dirigir_sus_equipos_y_al_reasignarlo_vuelve_sin_ninguno()
    {
        var (e, categoria, equipoA, equipoB) = await ConEntrenadorYDosEquiposAsync();
        var asignado = e.EntrenadorDe(categoria.Id, e.IntegranteEntrenador.Id);
        await e.Presidente.PutAsync(
            e.EquiposQueDirige(categoria.Id, e.IntegranteEntrenador.Id), new { equipoIds = new[] { equipoA.Id, equipoB.Id } });

        await e.Presidente.DeleteAsync(asignado);
        var reasignado = await e.Presidente.PutAsync(asignado, new { });

        Assert.Empty(await DirigeAsync(reasignado));
    }

    [Fact]
    public async Task Desactivar_la_categoria_borra_lo_que_dirigian_sus_entrenadores()
    {
        var (e, categoria, equipoA, _) = await ConEntrenadorYDosEquiposAsync();
        var asignado = e.EntrenadorDe(categoria.Id, e.IntegranteEntrenador.Id);
        await e.Presidente.PutAsync(
            e.EquiposQueDirige(categoria.Id, e.IntegranteEntrenador.Id), new { equipoIds = new[] { equipoA.Id } });

        await e.Presidente.PostAsync(e.Desactivacion(categoria.Id));
        var reactivada = await (await e.Presidente.PostAsync(e.Reactivacion(categoria.Id))).JsonAsync();
        var reasignado = await e.Presidente.PutAsync(asignado, new { });

        // Sus equipos vuelven con ella, vacíos y sin entrenador (caso límite de la spec).
        Assert.Equal(2, reactivada.GetProperty("categoria").Lista("equipos").Count);
        Assert.Empty(reactivada.GetProperty("categoria").Lista("entrenadores"));
        Assert.Empty(await DirigeAsync(reasignado));
    }

    [Fact]
    public async Task Solo_el_presidente_de_ese_club_indica_que_equipos_dirige_cada_entrenador()
    {
        var (e, categoria, equipoA, _) = await ConEntrenadorYDosEquiposAsync();
        var cuerpo = $"{{\"equipoIds\":[\"{equipoA.Id}\"]}}";

        foreach (var (_, cliente) in e.QuienesNoGestionan)
        {
            await AislamientoCategoriasPruebas.ComprobarAsync(cliente, HttpStatusCode.Forbidden, "rol_no_autorizado",
                ("PUT", e.EquiposQueDirige(categoria.Id, e.IntegranteEntrenador.Id), cuerpo));
        }

        await AislamientoCategoriasPruebas.ComprobarAsync(e.PresidenteDeOtroClub, HttpStatusCode.NotFound, "no_encontrado",
            ("PUT", $"/api/clubes/{e.OtroClub.Id}/categorias/{categoria.Id}/entrenadores/{e.IntegranteEntrenador.Id}/equipos", cuerpo));
        Assert.Empty(Dirige(await e.DetalleAsync(categoria.Id)));
    }

    private async Task<(EscenarioCategorias E, Categoria Categoria, Equipo A, Equipo B)> ConEntrenadorYDosEquiposAsync()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegranteEntrenador);
        return (e, categoria, await e.Sembrar.CrearEquipoAsync(categoria, "A"), await e.Sembrar.CrearEquipoAsync(categoria, "B"));
    }

    private static async Task<List<Guid>> DirigeAsync(HttpResponseMessage respuesta) => Dirige(await respuesta.JsonAsync());

    /// <summary>Equipos que dirige el único entrenador de la categoría, por nombre.</summary>
    private static List<Guid> Dirige(System.Text.Json.JsonElement detalle) =>
        detalle.Lista("entrenadores").Single().Lista("equipos").Ids("equipoId");
}
