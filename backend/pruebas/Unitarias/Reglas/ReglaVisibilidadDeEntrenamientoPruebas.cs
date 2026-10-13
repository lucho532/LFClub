using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaVisibilidadDeEntrenamientoPruebas
{
    private static bool LaVe(
        Rol rol,
        bool entrena = false,
        bool suCategoria = false,
        bool asistencia = false,
        bool tieneEquipos = false,
        bool enSusEquipos = false,
        bool enLaLista = false) =>
        ReglaVisibilidadDeEntrenamiento.LaVe(rol, entrena, suCategoria, asistencia, tieneEquipos, enSusEquipos, enLaLista);

    [Fact]
    public void El_presidente_ve_todos_tengan_o_no_equipos()
    {
        Assert.True(LaVe(Rol.PRESIDENTE));
        Assert.True(LaVe(Rol.PRESIDENTE, tieneEquipos: true));
    }

    [Fact]
    public void El_directivo_sin_asignar_ve_los_de_toda_la_categoria() =>
        Assert.True(LaVe(Rol.DIRECTIVO));

    [Fact]
    public void El_directivo_sin_asignar_no_ve_los_dirigidos_a_equipos() =>
        Assert.False(LaVe(Rol.DIRECTIVO, tieneEquipos: true));

    [Fact]
    public void El_directivo_asignado_ve_los_dirigidos_a_equipos_de_su_categoria() =>
        Assert.True(LaVe(Rol.DIRECTIVO, entrena: true, tieneEquipos: true));

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void El_entrenador_ve_los_que_tienen_alguna_categoria_que_entrena(bool entrena, bool tieneEquipos, bool esperado) =>
        Assert.Equal(esperado, LaVe(Rol.ENTRENADOR, entrena: entrena, tieneEquipos: tieneEquipos));

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public void El_jugador_ve_los_de_su_categoria_y_aquellos_con_asistencia(bool suCategoria, bool asistencia, bool esperado) =>
        Assert.Equal(esperado, LaVe(Rol.JUGADOR, suCategoria: suCategoria, asistencia: asistencia));

    [Fact]
    public void El_jugador_de_la_categoria_pero_de_otro_equipo_no_ve_el_dirigido_a_equipos() =>
        Assert.False(LaVe(Rol.JUGADOR, suCategoria: true, tieneEquipos: true));

    [Fact]
    public void El_jugador_de_uno_de_los_equipos_lo_ve() =>
        Assert.True(LaVe(Rol.JUGADOR, suCategoria: true, tieneEquipos: true, enSusEquipos: true));

    [Fact]
    public void El_jugador_de_un_equipo_de_otra_categoria_lo_ve_si_esta_en_el_equipo() =>
        Assert.True(LaVe(Rol.JUGADOR, tieneEquipos: true, enSusEquipos: true));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void El_anadido_lo_ve_aunque_no_sea_de_su_categoria_ni_de_sus_equipos(bool tieneEquipos) =>
        Assert.True(LaVe(Rol.JUGADOR, tieneEquipos: tieneEquipos, enLaLista: true));

    [Fact]
    public void El_jugador_con_asistencia_registrada_lo_sigue_viendo_aunque_ya_no_sea_de_la_audiencia() =>
        Assert.True(LaVe(Rol.JUGADOR, asistencia: true, tieneEquipos: true));

    [Fact]
    public void Al_jugador_no_le_basta_una_asignacion() =>
        Assert.False(LaVe(Rol.JUGADOR, entrena: true));

    [Fact]
    public void El_desarrollador_no_ve_ninguno() =>
        Assert.False(LaVe(Rol.DESARROLLADOR, entrena: true, suCategoria: true, asistencia: true, enLaLista: true));
}
