using System.Net;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// Desactivar, reactivar y borrar una categoría (constitución §14; RF-004 a RF-007; historia 1,
/// escenarios 4 a 6, 9 y 10). Va aparte de <see cref="GestionCategoriasPruebas"/> para que ningún
/// archivo supere las 250 líneas.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EstadoDeCategoriasPruebas
{
    private readonly FabricaApi _fabrica;

    public EstadoDeCategoriasPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Desactivar_una_categoria_sin_jugadores_le_quita_los_entrenadores_y_reactivarla_no_los_devuelve()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var equipo = await e.Sembrar.CrearEquipoAsync(categoria, "A");
        var asignacion = await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegranteEntrenador);
        await e.Sembrar.DirigirEquipoAsync(asignacion, equipo);

        var desactivada = await e.Presidente.PostAsync(e.Desactivacion(categoria.Id));
        var otraVez = await e.Presidente.PostAsync(e.Desactivacion(categoria.Id));

        Assert.Equal(HttpStatusCode.OK, desactivada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otraVez.StatusCode);
        var dto = await desactivada.JsonAsync();
        Assert.False(dto.GetProperty("activa").GetBoolean());
        Assert.Empty(dto.Lista("entrenadores"));
        Assert.False((await e.Presidente.ListaAsync(e.Categorias)).Single().GetProperty("activa").GetBoolean());

        // La asignación no se borra: deja de estar activa, y lo que dirigía sí se borra (RF-006, RF-021).
        var guardada = (await e.CategoriaGuardadaAsync(categoria.Id))!;
        Assert.False(guardada.Asignaciones.Single().Activa);
        Assert.Equal(0, await _fabrica.ConContextoAsync(contexto => Task.FromResult(
            contexto.EntrenadoresEquipo.IgnoreQueryFilters().Count(fila => fila.EquipoId == equipo.Id))));

        var reactivada = await (await e.Presidente.PostAsync(e.Reactivacion(categoria.Id))).JsonAsync();
        Assert.True(reactivada.GetProperty("categoria").GetProperty("activa").GetBoolean());
        Assert.Empty(reactivada.GetProperty("categoria").Lista("entrenadores"));
        Assert.Single(reactivada.GetProperty("categoria").Lista("equipos"));
    }

    [Fact]
    public async Task Una_categoria_con_jugadores_no_se_desactiva()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        await e.Sembrar.CrearJugadorAsync(e.Club, 2016, categoria);

        var respuesta = await e.Presidente.PostAsync(e.Desactivacion(categoria.Id));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("categoria_con_jugadores", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.True((await e.CategoriaGuardadaAsync(categoria.Id))!.Activa);
    }

    [Fact]
    public async Task Reactivar_recoge_a_los_jugadores_sin_categoria_de_su_anio_y_hacerlo_dos_veces_no_cambia_nada()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, EscenarioCategorias.AnioDelJugador, activa: false);

        var primera = await (await e.Presidente.PostAsync(e.Reactivacion(categoria.Id))).JsonAsync();
        var segunda = await e.Presidente.PostAsync(e.Reactivacion(categoria.Id));

        Assert.Equal(1, primera.GetProperty("jugadoresUbicados").GetInt32());
        Assert.Equal(1, primera.GetProperty("categoria").GetProperty("numeroJugadores").GetInt32());
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(0, (await segunda.JsonAsync()).GetProperty("jugadoresUbicados").GetInt32());
        Assert.Equal(categoria.Id, (await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!.CategoriaId);
    }

    [Fact]
    public async Task Una_categoria_que_nunca_se_uso_se_borra_con_sus_equipos_y_el_anio_queda_libre()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);
        var equipo = await e.Sembrar.CrearEquipoAsync((await e.CategoriaGuardadaAsync(categoriaId))!, "A");

        var respuesta = await e.Presidente.DeleteAsync(e.RutaCategoria(categoriaId));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Null(await e.CategoriaGuardadaAsync(categoriaId));
        Assert.False(await _fabrica.ConContextoAsync(contexto => Task.FromResult(
            contexto.Equipos.IgnoreQueryFilters().Any(fila => fila.Id == equipo.Id))));
        Assert.Empty(await e.Presidente.ListaAsync(e.Categorias));
        Assert.Equal(HttpStatusCode.Created, (await e.Presidente.PostAsync(e.Categorias, new { anio = 2015 })).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Una_categoria_que_tuvo_jugadores_o_entrenadores_no_se_borra_aunque_hoy_este_vacia(bool activa)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015, activa, usada: true);

        var respuesta = await e.Presidente.DeleteAsync(e.RutaCategoria(categoria.Id));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("categoria_con_historial", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Contains("desactivar", await ClienteDePrueba.TituloAsync(respuesta), StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(await e.CategoriaGuardadaAsync(categoria.Id));
    }

    [Fact]
    public async Task Una_categoria_que_no_existe_responde_404_en_las_tres_operaciones()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var inexistente = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await e.Presidente.PostAsync(e.Desactivacion(inexistente))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Presidente.PostAsync(e.Reactivacion(inexistente))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Presidente.DeleteAsync(e.RutaCategoria(inexistente))).StatusCode);
    }
}
