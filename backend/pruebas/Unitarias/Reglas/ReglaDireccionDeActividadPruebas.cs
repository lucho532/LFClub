using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaDireccionDeActividadPruebas
{
    private static readonly Guid C2014 = Guid.NewGuid();
    private static readonly Guid C2015 = Guid.NewGuid();
    private static readonly Guid C2016 = Guid.NewGuid();

    private static bool Dirige(Rol rol, Guid[] actividad, params Guid[] entrena) =>
        ReglaDireccionDeActividad.PuedeDirigir(rol, actividad, entrena.ToHashSet());

    [Fact]
    public void El_presidente_dirige_cualquier_actividad_sin_estar_asignado() =>
        Assert.True(Dirige(Rol.PRESIDENTE, [C2014, C2015]));

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public void Quien_entrena_alguna_categoria_de_la_actividad_la_dirige(Rol rol) =>
        Assert.True(Dirige(rol, [C2014, C2015], C2014));

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public void Quien_entrena_todas_las_categorias_tambien_la_dirige(Rol rol) =>
        Assert.True(Dirige(rol, [C2014, C2015], C2014, C2015));

    [Fact]
    public void El_directivo_sin_asignar_no_dirige() =>
        Assert.False(Dirige(Rol.DIRECTIVO, [C2014]));

    [Fact]
    public void El_entrenador_de_otra_categoria_no_dirige() =>
        Assert.False(Dirige(Rol.ENTRENADOR, [C2014], C2016));

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.DESARROLLADOR)]
    public void El_jugador_y_el_desarrollador_nunca_dirigen(Rol rol) =>
        Assert.False(Dirige(rol, [C2014], C2014));

    [Fact]
    public void Dirigir_es_mas_amplio_que_gestionar()
    {
        var entrena = new HashSet<Guid> { C2014 };
        var categorias = new[] { C2014, C2015 };

        Assert.True(ReglaDireccionDeActividad.PuedeDirigir(Rol.ENTRENADOR, categorias, entrena));
        Assert.False(ReglaGestionDeActividad.PuedeGestionar(Rol.ENTRENADOR, categorias, entrena));
    }
}
