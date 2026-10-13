using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaAudienciaDeEntrenamientoPruebas
{
    private static bool EnLaAudiencia(
        bool tieneEquipos = false,
        bool enSusEquipos = false,
        bool suCategoria = false,
        bool enLaLista = false,
        bool activo = true,
        bool tieneCategoria = true) =>
        ReglaAudienciaDeEntrenamiento.EnLaAudiencia(activo, tieneCategoria, tieneEquipos, enSusEquipos, suCategoria, enLaLista);

    [Fact]
    public void Sin_equipos_entra_por_su_categoria() =>
        Assert.True(EnLaAudiencia(suCategoria: true));

    [Fact]
    public void Sin_equipos_entra_el_jugador_sin_equipo() =>
        Assert.True(EnLaAudiencia(tieneEquipos: false, enSusEquipos: false, suCategoria: true));

    [Fact]
    public void Sin_equipos_no_entra_el_de_otra_categoria() =>
        Assert.False(EnLaAudiencia());

    [Fact]
    public void Con_equipos_entra_por_estar_en_alguno() =>
        Assert.True(EnLaAudiencia(tieneEquipos: true, enSusEquipos: true, suCategoria: true));

    [Fact]
    public void Con_equipos_la_categoria_sola_no_basta() =>
        Assert.False(EnLaAudiencia(tieneEquipos: true, suCategoria: true));

    [Fact]
    public void Con_equipos_entra_el_de_un_equipo_aunque_su_categoria_no_este_entre_las_del_entrenamiento() =>
        Assert.True(EnLaAudiencia(tieneEquipos: true, enSusEquipos: true));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void El_de_la_lista_guardada_entra_siempre(bool tieneEquipos) =>
        Assert.True(EnLaAudiencia(tieneEquipos: tieneEquipos, enLaLista: true));

    [Fact]
    public void Entra_una_sola_vez_aunque_lo_haga_por_varios_caminos() =>
        Assert.True(EnLaAudiencia(tieneEquipos: true, enSusEquipos: true, suCategoria: true, enLaLista: true));

    [Fact]
    public void El_inactivo_o_retirado_no_entra_por_ningun_camino() =>
        Assert.False(EnLaAudiencia(tieneEquipos: true, enSusEquipos: true, suCategoria: true, enLaLista: true, activo: false));

    [Fact]
    public void El_jugador_sin_categoria_no_entra_ni_estando_en_la_lista() =>
        Assert.False(EnLaAudiencia(enSusEquipos: true, suCategoria: true, enLaLista: true, tieneCategoria: false));
}
