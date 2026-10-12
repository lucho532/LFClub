using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

// La tabla de research §8, fila por fila.
public class ReglaAccesoACartaPruebas
{
    [Fact]
    public void El_presidente_cambia_todo_y_exporta() =>
        Assert.Equal(new AlcanceDeCarta(true, true, true), ReglaAccesoACarta.Alcance(Rol.PRESIDENTE, false, false, true));

    [Fact]
    public void El_presidente_sobre_un_retirado_solo_exporta() =>
        Assert.Equal(new AlcanceDeCarta(false, false, true), ReglaAccesoACarta.Alcance(Rol.PRESIDENTE, false, false, false));

    [Fact]
    public void El_presidente_que_entrena_la_categoria_sigue_exportando() =>
        Assert.Equal(new AlcanceDeCarta(true, true, true), ReglaAccesoACarta.Alcance(Rol.PRESIDENTE, false, true, true));

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public void Quien_entrena_su_categoria_cambia_todo_y_no_exporta(Rol rol) =>
        Assert.Equal(new AlcanceDeCarta(true, true, false), ReglaAccesoACarta.Alcance(rol, false, true, true));

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public void Quien_no_entrena_su_categoria_solo_mira(Rol rol) =>
        Assert.Equal(AlcanceDeCarta.Ninguno, ReglaAccesoACarta.Alcance(rol, false, false, true));

    [Fact]
    public void Quien_entrena_no_cambia_la_de_un_retirado() =>
        Assert.Equal(AlcanceDeCarta.Ninguno, ReglaAccesoACarta.Alcance(Rol.DIRECTIVO, false, true, false));

    [Fact]
    public void La_familia_cambia_dorsal_y_posicion_y_exporta_pero_nunca_las_estadisticas() =>
        Assert.Equal(new AlcanceDeCarta(false, true, true), ReglaAccesoACarta.Alcance(Rol.JUGADOR, true, false, true));

    [Fact]
    public void Otra_cuenta_de_jugador_o_el_desarrollador_no_pueden_nada()
    {
        Assert.Equal(AlcanceDeCarta.Ninguno, ReglaAccesoACarta.Alcance(Rol.JUGADOR, false, false, true));
        Assert.Equal(AlcanceDeCarta.Ninguno, ReglaAccesoACarta.Alcance(Rol.DESARROLLADOR, false, true, true));
    }
}
