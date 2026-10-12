using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaGestionDeActividadPruebas
{
    private static readonly Guid C2014 = Guid.NewGuid();
    private static readonly Guid C2015 = Guid.NewGuid();
    private static readonly Guid C2016 = Guid.NewGuid();

    private static bool Gestiona(Rol rol, Guid[] actividad, params Guid[] entrena) =>
        ReglaGestionDeActividad.PuedeGestionar(rol, actividad, entrena.ToHashSet());

    [Fact]
    public void El_presidente_gestiona_cualquier_actividad_sin_estar_asignado() =>
        Assert.True(Gestiona(Rol.PRESIDENTE, [C2014, C2015, C2016]));

    [Fact]
    public void El_presidente_asignado_a_una_sola_categoria_gestiona_tambien_las_demas() =>
        Assert.True(Gestiona(Rol.PRESIDENTE, [C2016], C2014));

    [Fact]
    public void El_entrenador_de_la_2014_gestiona_una_actividad_de_la_2014() =>
        Assert.True(Gestiona(Rol.ENTRENADOR, [C2014], C2014));

    [Fact]
    public void El_entrenador_de_la_2014_no_gestiona_una_conjunta_de_2014_y_2015() =>
        Assert.False(Gestiona(Rol.ENTRENADOR, [C2014, C2015], C2014));

    [Fact]
    public void El_entrenador_de_2014_y_2015_gestiona_la_conjunta() =>
        Assert.True(Gestiona(Rol.ENTRENADOR, [C2014, C2015], C2014, C2015));

    [Fact]
    public void El_entrenador_de_la_2014_no_gestiona_una_de_la_2016() =>
        Assert.False(Gestiona(Rol.ENTRENADOR, [C2016], C2014));

    [Fact]
    public void El_directivo_asignado_gestiona_como_un_entrenador()
    {
        Assert.True(Gestiona(Rol.DIRECTIVO, [C2014], C2014));
        Assert.False(Gestiona(Rol.DIRECTIVO, [C2014, C2015], C2014));
    }

    [Fact]
    public void El_directivo_sin_asignar_no_gestiona_nada() =>
        Assert.False(Gestiona(Rol.DIRECTIVO, [C2014]));

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.DESARROLLADOR)]
    public void El_jugador_y_el_desarrollador_nunca_gestionan(Rol rol) =>
        Assert.False(Gestiona(rol, [C2014], C2014));

    [Fact]
    public void Una_actividad_sin_categorias_no_la_gestiona_nadie() =>
        Assert.False(Gestiona(Rol.PRESIDENTE, []));
}
