using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaVisibilidadDeEntrenamientoPruebas
{
    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    public void El_presidente_y_el_directivo_ven_todos(Rol rol) =>
        Assert.True(ReglaVisibilidadDeEntrenamiento.LaVe(rol, false, false, false));

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void El_entrenador_ve_los_que_tienen_alguna_categoria_que_entrena(bool entrena, bool esperado) =>
        Assert.Equal(esperado, ReglaVisibilidadDeEntrenamiento.LaVe(Rol.ENTRENADOR, entrena, false, false));

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public void El_jugador_ve_los_de_su_categoria_y_aquellos_con_asistencia(bool suCategoria, bool asistencia, bool esperado) =>
        Assert.Equal(esperado, ReglaVisibilidadDeEntrenamiento.LaVe(Rol.JUGADOR, false, suCategoria, asistencia));

    [Fact]
    public void Al_jugador_no_le_basta_una_asignacion() =>
        Assert.False(ReglaVisibilidadDeEntrenamiento.LaVe(Rol.JUGADOR, true, false, false));

    [Fact]
    public void El_desarrollador_no_ve_ninguno() =>
        Assert.False(ReglaVisibilidadDeEntrenamiento.LaVe(Rol.DESARROLLADOR, true, true, true));
}
