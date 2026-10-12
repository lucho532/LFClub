using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Aislamiento;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Categorias;

/// <summary>
/// En un club suspendido su PRESIDENTE sigue gestionando las categorías y los demás no entran; en
/// uno dado de baja nadie las consulta ni las gestiona (constitución §7.4, RF-039). Lo resuelve la
/// autorización de la 001; aquí se comprueba sobre los endpoints de esta funcionalidad.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CategoriasPorEstadoDelClubPruebas
{
    private readonly FabricaApi _fabrica;

    public CategoriasPorEstadoDelClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// Los 22 endpoints de la funcionalidad, tal como los expone la API, más los 4 de la 007 que
    /// comparten su prefijo (la lista "Jugadores" y la foto del jugador). Los de la ficha del jugador
    /// (005) comparten el prefijo de ruta y se prueban en <c>Ficha/FichaPorEstadoDelClubPruebas</c>; el
    /// de agregar un hermano (006), en <c>Hermanos/HermanosEntreClubesPruebas</c>. Los de la carta
    /// (010) quedan fuera: son de otra funcionalidad y los cubren las pruebas de <c>Carta/</c>.
    /// </summary>
    private List<Endpoint> DeCategorias => EndpointsDeLaApi.DeLaApi(_fabrica)
        .Where(endpoint => new[] { "/categorias", "/jugadores", "/mi-categoria" }
            .Any(parte => endpoint.Ruta.StartsWith("/api/clubes/{clubId}" + parte, StringComparison.Ordinal))
            && !endpoint.Ruta.Contains("/ficha", StringComparison.Ordinal)
            && !endpoint.Ruta.EndsWith("/hermanos", StringComparison.Ordinal)
            && !endpoint.Ruta.EndsWith("/estadisticas", StringComparison.Ordinal)
            && !endpoint.Ruta.Contains("/carta", StringComparison.Ordinal))
        .ToList();

    [Fact]
    public async Task En_un_club_suspendido_el_presidente_sigue_gestionando_las_categorias()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await CambiarEstadoAsync(e, EstadoClub.SUSPENDIDO);

        var categoriaId = await e.CrearCategoriaPorApiAsync(EscenarioCategorias.AnioDelJugador);
        var otraId = await e.CrearCategoriaPorApiAsync(2016);
        var conEntrenador = await e.Presidente.PutAsync(e.EntrenadorDe(categoriaId, e.IntegranteEntrenador.Id), new { });
        var conEquipo = await e.Presidente.PostAsync(e.Equipos(categoriaId), new { nombre = "A" });
        var movido = await e.Presidente.PutAsync(e.CategoriaDe(e.IntegranteJugador.Id), new { categoriaId = otraId });
        var retirado = await e.Presidente.PostAsync(e.Retiro(e.IntegranteJugador.Id));
        var desactivada = await e.Presidente.PostAsync(e.Desactivacion(otraId));
        var reactivada = await e.Presidente.PostAsync(e.Reactivacion(otraId));

        Assert.Equal(HttpStatusCode.OK, conEntrenador.StatusCode);
        Assert.Equal(HttpStatusCode.Created, conEquipo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, movido.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, retirado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, desactivada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reactivada.StatusCode);
        Assert.Equal(2, (await e.Presidente.ListaAsync(e.Categorias)).Count);
    }

    [Fact]
    public async Task En_un_club_suspendido_los_demas_reciben_el_aviso_en_los_26_endpoints()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (usuarioRetirado, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);
        var delRetirado = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuarioRetirado);
        await CambiarEstadoAsync(e, EstadoClub.SUSPENDIDO);
        Assert.Equal(26, DeCategorias.Count);

        // También el retirado: primero se mira el estado del club (supuesto 6).
        foreach (var cliente in new[] { e.Directivo, e.Entrenador, e.Jugador, delRetirado })
        {
            await ComprobarEnTodosAsync(cliente, e, "club_suspendido");
        }
    }

    [Fact]
    public async Task En_un_club_dado_de_baja_nadie_consulta_ni_gestiona_y_al_revertir_todo_sigue_como_estaba()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var equipo = await e.Sembrar.CrearEquipoAsync(categoria, "A");
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, categoria);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);
        await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegranteEntrenador);
        var antes = (await e.Presidente.GetAsync(e.RutaCategoria(categoria.Id))).Content.ReadAsStringAsync();
        await CambiarEstadoAsync(e, EstadoClub.DADO_DE_BAJA);

        foreach (var cliente in new[] { e.Presidente, e.Directivo, e.Entrenador, e.Jugador })
        {
            await ComprobarEnTodosAsync(cliente, e, "club_dado_de_baja");
        }

        await CambiarEstadoAsync(e, EstadoClub.ACTIVO);
        var despues = await (await e.Presidente.GetAsync(e.RutaCategoria(categoria.Id))).Content.ReadAsStringAsync();
        Assert.Equal(await antes, despues);
    }

    private async Task ComprobarEnTodosAsync(ClienteDePrueba cliente, EscenarioCategorias e, string codigo)
    {
        foreach (var endpoint in DeCategorias)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(cliente, endpoint, e.Club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(e.Club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }
    }

    private Task<int> CambiarEstadoAsync(EscenarioCategorias e, EstadoClub estado) => _fabrica.ConContextoAsync(contexto =>
        contexto.Clubes.Where(club => club.Id == e.Club.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(club => club.Estado, estado)));
}
