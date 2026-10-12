using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaAlcanceDeAsistenciaPruebas
{
    private static readonly Guid C2014 = Guid.NewGuid();
    private static readonly Guid C2015 = Guid.NewGuid();
    private static readonly Guid C2016 = Guid.NewGuid();

    private static IReadOnlySet<Guid> Marcables(Rol rol, Guid[] entrenamiento, params Guid[] entrena) =>
        ReglaAlcanceDeAsistencia.CategoriasMarcables(rol, entrenamiento, entrena.ToHashSet());

    [Fact]
    public void El_presidente_sin_asignacion_marca_todas() =>
        Assert.True(Marcables(Rol.PRESIDENTE, [C2014, C2015]).SetEquals([C2014, C2015]));

    [Fact]
    public void El_entrenador_de_la_2014_marca_solo_la_2014_en_una_conjunta() =>
        Assert.Equal([C2014], Marcables(Rol.ENTRENADOR, [C2014, C2015], C2014));

    [Fact]
    public void El_entrenador_de_2014_y_2015_marca_las_dos() =>
        Assert.Equal(2, Marcables(Rol.ENTRENADOR, [C2014, C2015], C2014, C2015).Count);

    [Fact]
    public void El_entrenador_de_la_2014_no_marca_nada_en_uno_de_la_2016() =>
        Assert.Empty(Marcables(Rol.ENTRENADOR, [C2016], C2014));

    [Fact]
    public void El_directivo_asignado_marca_lo_suyo_y_el_sin_asignar_nada()
    {
        Assert.Equal([C2014], Marcables(Rol.DIRECTIVO, [C2014, C2015], C2014));
        Assert.Empty(Marcables(Rol.DIRECTIVO, [C2014, C2015]));
    }

    [Fact]
    public void El_presidente_asignado_a_una_sola_marca_todas() =>
        Assert.Equal(2, Marcables(Rol.PRESIDENTE, [C2014, C2015], C2014).Count);

    [Fact]
    public void El_jugador_no_marca_nada() =>
        Assert.Empty(Marcables(Rol.JUGADOR, [C2014], C2014));
}
