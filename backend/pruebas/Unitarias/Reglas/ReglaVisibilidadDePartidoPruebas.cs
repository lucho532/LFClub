using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaVisibilidadDePartidoPruebas
{
    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public void Un_partido_jugado_lo_ve_todo_el_club(Rol rol) =>
        Assert.True(ReglaVisibilidadDePartido.LoVe(rol, EstadoPartido.JUGADO, false, false));

    [Theory]
    [InlineData(EstadoPartido.PROXIMO)]
    [InlineData(EstadoPartido.CANCELADO)]
    public void El_presidente_ve_los_proximos_y_cancelados_sin_estar_asignado(EstadoPartido estado) =>
        Assert.True(ReglaVisibilidadDePartido.LoVe(Rol.PRESIDENTE, estado, false, false));

    [Theory]
    [InlineData(Rol.ENTRENADOR, EstadoPartido.PROXIMO, true, true)]
    [InlineData(Rol.ENTRENADOR, EstadoPartido.PROXIMO, false, false)]
    [InlineData(Rol.ENTRENADOR, EstadoPartido.CANCELADO, true, true)]
    [InlineData(Rol.ENTRENADOR, EstadoPartido.CANCELADO, false, false)]
    [InlineData(Rol.DIRECTIVO, EstadoPartido.PROXIMO, true, true)]
    [InlineData(Rol.DIRECTIVO, EstadoPartido.PROXIMO, false, false)]
    [InlineData(Rol.DIRECTIVO, EstadoPartido.CANCELADO, true, true)]
    [InlineData(Rol.DIRECTIVO, EstadoPartido.CANCELADO, false, false)]
    public void Quien_entrena_alguna_de_sus_categorias_ve_el_proximo_y_el_cancelado(
        Rol rol, EstadoPartido estado, bool entrena, bool esperado) =>
        Assert.Equal(esperado, ReglaVisibilidadDePartido.LoVe(rol, estado, entrena, false));

    [Fact]
    public void El_directivo_sin_asignar_no_ve_un_proximo_y_si_un_jugado()
    {
        Assert.False(ReglaVisibilidadDePartido.LoVe(Rol.DIRECTIVO, EstadoPartido.PROXIMO, false, false));
        Assert.True(ReglaVisibilidadDePartido.LoVe(Rol.DIRECTIVO, EstadoPartido.JUGADO, false, false));
    }

    [Theory]
    [InlineData(EstadoPartido.PROXIMO, true, true)]
    [InlineData(EstadoPartido.PROXIMO, false, false)]
    [InlineData(EstadoPartido.CANCELADO, true, true)]
    [InlineData(EstadoPartido.CANCELADO, false, false)]
    [InlineData(EstadoPartido.JUGADO, false, true)]
    public void El_jugador_ve_el_proximo_y_el_cancelado_solo_si_esta_convocado(
        EstadoPartido estado, bool convocado, bool esperado) =>
        Assert.Equal(esperado, ReglaVisibilidadDePartido.LoVe(Rol.JUGADOR, estado, false, convocado));

    [Theory]
    [InlineData(EstadoPartido.PROXIMO)]
    [InlineData(EstadoPartido.CANCELADO)]
    [InlineData(EstadoPartido.JUGADO)]
    public void El_desarrollador_no_ve_ninguno(EstadoPartido estado) =>
        Assert.False(ReglaVisibilidadDePartido.LoVe(Rol.DESARROLLADOR, estado, true, true));
}
