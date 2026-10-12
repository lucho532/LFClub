using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LFClub.Pruebas.Integracion.Personas;

/// <summary>
/// La ficha básica no cruza clubes: desde otro club o como DESARROLLADOR es como si no existiera, y
/// la de una persona con roles en dos clubes solo muestra lo de cada uno (spec 008, RF-004, RF-005 y
/// RF-020; CE-002).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FichaBasicaEntreClubesPruebas
{
    private readonly FabricaApi _fabrica;

    public FichaBasicaEntreClubesPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_presidente_de_otro_club_recibe_404_sin_el_nombre_de_E1()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var presidenteB = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.PresidenteB.Usuario);

        var respuesta = await presidenteB.GetAsync(c.Ficha(c.E1.Integrante));

        await NoEncontradoAsync(respuesta);
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Ernesto", texto);
        Assert.DoesNotContain(c.E1.Integrante.Apellidos, texto);
    }

    [Fact]
    public async Task El_desarrollador_recibe_404()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        await NoEncontradoAsync(await desarrollador.GetAsync(c.Ficha(c.E1.Integrante)));
    }

    [Fact]
    public async Task En_el_club_B_E1_es_presidente_y_no_lleva_las_categorias_de_A()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var presidenteB = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.PresidenteB.Usuario);

        var ficha = await LeerAsync(presidenteB, $"/api/clubes/{c.ClubB.Id}/personas/{c.E1EnB.Id}");

        Assert.Equal("PRESIDENTE", ficha.GetProperty("rol").GetString());
        Assert.Empty(CategoriasDe(ficha));
        var enA = await LeerAsync(await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.E1.Usuario), c.Ficha(c.E1.Integrante));
        Assert.Equal(enA.GetProperty("versionFoto").GetInt32(), ficha.GetProperty("versionFoto").GetInt32());
    }

    [Fact]
    public async Task Con_el_identificador_de_A_en_la_ruta_de_B_no_hay_ficha()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var e1 = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.E1.Usuario);

        await NoEncontradoAsync(await e1.GetAsync($"/api/clubes/{c.ClubB.Id}/personas/{c.E1.Integrante.Id}"));
    }
}
