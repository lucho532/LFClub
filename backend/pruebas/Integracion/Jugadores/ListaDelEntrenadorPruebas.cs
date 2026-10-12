using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Jugadores.ListaJugadoresPruebas;

namespace LFClub.Pruebas.Integracion.Jugadores;

/// <summary>
/// Un ENTRENADOR solo ve en la lista "Jugadores" a los de las categorías activas que entrena,
/// consultando su asignación en cada petición (spec 007, historia 1; RF-003 y RF-004; CE-002).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ListaDelEntrenadorPruebas
{
    private readonly FabricaApi _fabrica;

    public ListaDelEntrenadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Ve_solo_su_categoria_con_todos_sus_jugadores_aunque_no_esten_en_su_equipo()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();

        var grupos = await ListarAsync(await _fabrica.Jugadores.ClienteAsync(c.E1.Usuario), c);

        // Beto no está en el equipo "A" que dirige, pero sí en su categoría; sin 2015 ni "Sin categoría".
        var grupo = Assert.Single(grupos);
        Assert.Equal(2014, AnioDe(grupo));
        Assert.Equal([c.Ana.Integrante.Id, c.Beto.Integrante.Id], IdsDe(grupo));
    }

    [Fact]
    public async Task Sin_categorias_asignadas_recibe_una_lista_vacia()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();

        Assert.Empty(await ListarAsync(await _fabrica.Jugadores.ClienteAsync(c.E2.Usuario), c));
    }

    [Fact]
    public async Task Al_perder_la_asignacion_la_siguiente_peticion_viene_vacia()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var cliente = await _fabrica.Jugadores.ClienteAsync(c.E1.Usuario);
        Assert.Single(await ListarAsync(cliente, c));

        await _fabrica.ConContextoAsync(contexto => contexto.AsignacionesEntrenadorCategoria.IgnoreQueryFilters()
            .Where(asignacion => asignacion.Id == c.AsignacionE1.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(asignacion => asignacion.Activa, false)));

        Assert.Empty(await ListarAsync(cliente, c));
    }

    [Fact]
    public async Task Una_categoria_desactivada_deja_de_verse_aunque_siga_asignado()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        await _fabrica.Categorias.AsignarEntrenadorAsync(c.C2016, c.E2.Integrante);
        await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2016, c.C2016);

        Assert.Empty(await ListarAsync(await _fabrica.Jugadores.ClienteAsync(c.E2.Usuario), c));
    }

    [Fact]
    public async Task El_directivo_que_entrena_una_categoria_sigue_recibiendo_la_lista_completa()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();

        var grupos = await ListarAsync(await _fabrica.Jugadores.ClienteAsync(c.Directivo.Usuario), c);

        Assert.Equal([2014, 2015, null], grupos.Select(AnioDe).ToList());
    }
}
