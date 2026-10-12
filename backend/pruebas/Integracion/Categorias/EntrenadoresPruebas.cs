using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// El PRESIDENTE asigna y retira entrenadores de una categoría (constitución §8; RF-017 a RF-021;
/// historia 3).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EntrenadoresPruebas
{
    private readonly FabricaApi _fabrica;

    public EntrenadoresPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Los_candidatos_son_los_entrenadores_directivos_y_presidentes_aprobados_que_no_estan_asignados()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var (_, yaAsignado) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.ENTRENADOR);
        await e.Sembrar.AsignarEntrenadorAsync(categoria, yaAsignado);
        var (_, retiradoDeElla) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.ENTRENADOR);
        await e.Sembrar.AsignarEntrenadorAsync(categoria, retiradoDeElla, activa: false);
        await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 1990);
        await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, Rol.ENTRENADOR);

        var candidatos = await e.Presidente.ListaAsync(e.Candidatos(categoria.Id));

        // Incluye al propio presidente; no incluye al jugador, a quien está en espera ni a otro club.
        Assert.Equal(
            new[] { e.IntegrantePresidente.Id, e.IntegranteDirectivo.Id, e.IntegranteEntrenador.Id, retiradoDeElla.Id }.Order(),
            candidatos.Ids("usuarioRolId").Order());
        Assert.Contains("PRESIDENTE", candidatos.Select(candidato => candidato.GetProperty("rol").GetString()));
    }

    [Fact]
    public async Task Un_entrenador_puede_tener_varias_categorias_y_una_categoria_varios_entrenadores()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var una = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);

        var respuesta = await e.Presidente.PutAsync(e.EntrenadorDe(una.Id, e.IntegranteEntrenador.Id), new { });
        await e.Presidente.PutAsync(e.EntrenadorDe(una.Id, e.IntegrantePresidente.Id), new { });
        await e.Presidente.PutAsync(e.EntrenadorDe(otra.Id, e.IntegranteEntrenador.Id), new { });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal([e.IntegranteEntrenador.Id], (await respuesta.JsonAsync()).Lista("entrenadores").Ids("usuarioRolId"));
        var deUna = (await e.DetalleAsync(una.Id)).Lista("entrenadores");
        Assert.Equal(
            new[] { e.IntegranteEntrenador.Id, e.IntegrantePresidente.Id }.Order(), deUna.Ids("usuarioRolId").Order());
        Assert.All(deUna, entrenador => Assert.Empty(entrenador.Lista("equipos")));
        Assert.Equal([e.IntegranteEntrenador.Id], (await e.DetalleAsync(otra.Id)).Lista("entrenadores").Ids("usuarioRolId"));

        // Quien ya está asignado deja de ofrecerse.
        Assert.DoesNotContain(
            e.IntegranteEntrenador.Id, (await e.Presidente.ListaAsync(e.Candidatos(una.Id))).Ids("usuarioRolId"));
    }

    [Fact]
    public async Task Retirar_lo_quita_de_esa_categoria_conserva_las_demas_y_no_borra_la_asignacion()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var una = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        await e.Sembrar.AsignarEntrenadorAsync(una, e.IntegranteEntrenador);
        await e.Sembrar.AsignarEntrenadorAsync(otra, e.IntegranteEntrenador);

        var respuesta = await e.Presidente.DeleteAsync(e.EntrenadorDe(una.Id, e.IntegranteEntrenador.Id));
        var otraVez = await e.Presidente.DeleteAsync(e.EntrenadorDe(una.Id, e.IntegranteEntrenador.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otraVez.StatusCode);
        Assert.Empty((await respuesta.JsonAsync()).Lista("entrenadores"));
        Assert.Single((await e.DetalleAsync(otra.Id)).Lista("entrenadores"));
        Assert.False((await e.CategoriaGuardadaAsync(una.Id))!.Asignaciones.Single().Activa);
        Assert.Equal(Rol.ENTRENADOR, (await e.IntegranteGuardadoAsync(e.IntegranteEntrenador.Id))!.Rol);

        // RF-004a: aunque hoy no tenga a nadie, tuvo un entrenador y ya solo se desactiva.
        Assert.False((await e.DetalleAsync(una.Id)).GetProperty("sePuedeBorrar").GetBoolean());
    }

    [Fact]
    public async Task Asignar_dos_veces_o_reasignar_tras_retirar_deja_una_sola_asignacion()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);
        var ruta = e.EntrenadorDe(categoriaId, e.IntegranteEntrenador.Id);

        await e.Presidente.PutAsync(ruta, new { });
        var repetida = await e.Presidente.PutAsync(ruta, new { });
        var idInicial = (await e.CategoriaGuardadaAsync(categoriaId))!.Asignaciones.Single().Id;
        await e.Presidente.DeleteAsync(ruta);
        var reasignada = await e.Presidente.PutAsync(ruta, new { });

        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        Assert.Single((await repetida.JsonAsync()).Lista("entrenadores"));
        Assert.Single((await reasignada.JsonAsync()).Lista("entrenadores"));
        var asignacion = (await e.CategoriaGuardadaAsync(categoriaId))!.Asignaciones.Single();
        Assert.True(asignacion.Activa);
        Assert.Equal(idInicial, asignacion.Id);
    }

    [Fact]
    public async Task Un_jugador_y_quien_esta_en_espera_no_son_asignables_y_alguien_de_otro_club_no_existe()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);
        var (_, enEspera) = await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 1990);
        var (_, deOtroClub) = await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, Rol.ENTRENADOR);

        foreach (var noAsignable in new[] { e.IntegranteJugador.Id, enEspera.Id })
        {
            var respuesta = await e.Presidente.PutAsync(e.EntrenadorDe(categoriaId, noAsignable), new { });
            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal("no_asignable_como_entrenador", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        foreach (var inexistente in new[] { deOtroClub.Id, Guid.NewGuid() })
        {
            var respuesta = await e.Presidente.PutAsync(e.EntrenadorDe(categoriaId, inexistente), new { });
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Empty((await e.DetalleAsync(categoriaId)).Lista("entrenadores"));
        Assert.True((await e.DetalleAsync(categoriaId)).GetProperty("sePuedeBorrar").GetBoolean());
    }

    [Fact]
    public async Task A_una_categoria_inactiva_no_se_le_asigna_nadie()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var inactiva = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015, activa: false);

        var respuesta = await e.Presidente.PutAsync(e.EntrenadorDe(inactiva.Id, e.IntegranteEntrenador.Id), new { });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("categoria_inactiva", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Empty((await e.CategoriaGuardadaAsync(inactiva.Id))!.Asignaciones);
    }

    [Fact]
    public async Task Solo_el_presidente_ve_candidatos_asigna_y_retira_y_nadie_de_otro_club_lo_hace()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var (_, asignado) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.ENTRENADOR);
        await e.Sembrar.AsignarEntrenadorAsync(categoria, asignado);
        var enSuClub = $"/api/clubes/{e.OtroClub.Id}/categorias/{categoria.Id}/entrenadores";

        foreach (var (_, cliente) in e.QuienesNoGestionan)
        {
            await AislamientoCategoriasPruebas.ComprobarAsync(cliente, HttpStatusCode.Forbidden, "rol_no_autorizado",
                ("GET", e.Candidatos(categoria.Id), null),
                ("PUT", e.EntrenadorDe(categoria.Id, e.IntegranteEntrenador.Id), null),
                ("DELETE", e.EntrenadorDe(categoria.Id, asignado.Id), null));
        }

        await AislamientoCategoriasPruebas.ComprobarAsync(e.PresidenteDeOtroClub, HttpStatusCode.NotFound, "no_encontrado",
            ("GET", $"{enSuClub}/candidatos", null),
            ("PUT", $"{enSuClub}/{e.IntegranteEntrenador.Id}", null),
            ("DELETE", $"{enSuClub}/{asignado.Id}", null));

        Assert.Equal([asignado.Id], (await e.DetalleAsync(categoria.Id)).Lista("entrenadores").Ids("usuarioRolId"));
    }
}
