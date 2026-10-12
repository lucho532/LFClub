using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaAudienciaDeComunicadoPruebas
{
    private static readonly Guid C2014 = Guid.NewGuid();
    private static readonly Guid C2015 = Guid.NewGuid();
    private static readonly Guid[] SinCategorias = [];

    private static ResultadoAudiencia Evaluar(
        Rol rol, AlcanceComunicado alcance, Guid[] categorias, Guid[] entrena, Guid[] categoriasDeEquipos) =>
        ReglaAudienciaDeComunicado.Validar(rol, alcance, categorias, entrena, categoriasDeEquipos);

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    public void El_presidente_y_el_directivo_publican_a_cualquier_audiencia(Rol rol)
    {
        Assert.Equal(ResultadoAudiencia.Permitida, Evaluar(rol, AlcanceComunicado.CLUB, SinCategorias, [], []));
        Assert.Equal(ResultadoAudiencia.Permitida, Evaluar(rol, AlcanceComunicado.CATEGORIAS, [C2014, C2015], [], []));
        Assert.Equal(ResultadoAudiencia.Permitida, Evaluar(rol, AlcanceComunicado.EQUIPOS, SinCategorias, [], [C2014, C2014]));
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.DESARROLLADOR)]
    public void El_jugador_y_el_desarrollador_nunca_publican(Rol rol) =>
        Assert.Equal(ResultadoAudiencia.RolNoPublica, Evaluar(rol, AlcanceComunicado.CLUB, SinCategorias, [], []));

    [Fact]
    public void El_entrenador_publica_a_sus_categorias_y_a_equipos_de_ellas()
    {
        Assert.Equal(
            ResultadoAudiencia.Permitida,
            Evaluar(Rol.ENTRENADOR, AlcanceComunicado.CATEGORIAS, [C2014], [C2014], []));
        Assert.Equal(
            ResultadoAudiencia.Permitida,
            Evaluar(Rol.ENTRENADOR, AlcanceComunicado.EQUIPOS, SinCategorias, [C2014], [C2014, C2014]));
    }

    [Fact]
    public void El_entrenador_no_publica_a_una_categoria_ajena_ni_a_medias()
    {
        Assert.Equal(
            ResultadoAudiencia.FueraDeSuAlcance,
            Evaluar(Rol.ENTRENADOR, AlcanceComunicado.CATEGORIAS, [C2015], [C2014], []));
        Assert.Equal(
            ResultadoAudiencia.FueraDeSuAlcance,
            Evaluar(Rol.ENTRENADOR, AlcanceComunicado.CATEGORIAS, [C2014, C2015], [C2014], []));
        Assert.Equal(
            ResultadoAudiencia.FueraDeSuAlcance,
            Evaluar(Rol.ENTRENADOR, AlcanceComunicado.EQUIPOS, SinCategorias, [C2014], [C2015]));
    }

    [Fact]
    public void El_entrenador_sin_asignacion_no_publica_a_nadie() =>
        Assert.Equal(
            ResultadoAudiencia.FueraDeSuAlcance,
            Evaluar(Rol.ENTRENADOR, AlcanceComunicado.CATEGORIAS, [C2014], [], []));

    [Fact]
    public void El_entrenador_nunca_publica_a_todo_el_club() =>
        Assert.Equal(
            ResultadoAudiencia.FueraDeSuAlcance,
            Evaluar(Rol.ENTRENADOR, AlcanceComunicado.CLUB, SinCategorias, [C2014, C2015], []));

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.ENTRENADOR)]
    public void Equipos_de_dos_categorias_son_una_audiencia_invalida(Rol rol) =>
        Assert.Equal(
            ResultadoAudiencia.AudienciaInvalida,
            Evaluar(rol, AlcanceComunicado.EQUIPOS, SinCategorias, [C2014, C2015], [C2014, C2015]));

    [Fact]
    public void Las_combinaciones_incoherentes_son_una_audiencia_invalida()
    {
        // CLUB con categorías o con equipos.
        Assert.Equal(ResultadoAudiencia.AudienciaInvalida, Evaluar(Rol.PRESIDENTE, AlcanceComunicado.CLUB, [C2014], [], []));
        Assert.Equal(ResultadoAudiencia.AudienciaInvalida, Evaluar(Rol.PRESIDENTE, AlcanceComunicado.CLUB, SinCategorias, [], [C2014]));
        // CATEGORIAS sin categorías o con equipos.
        Assert.Equal(ResultadoAudiencia.AudienciaInvalida, Evaluar(Rol.PRESIDENTE, AlcanceComunicado.CATEGORIAS, SinCategorias, [], []));
        Assert.Equal(ResultadoAudiencia.AudienciaInvalida, Evaluar(Rol.PRESIDENTE, AlcanceComunicado.CATEGORIAS, [C2014], [], [C2014]));
        // EQUIPOS sin equipos o con categorías.
        Assert.Equal(ResultadoAudiencia.AudienciaInvalida, Evaluar(Rol.PRESIDENTE, AlcanceComunicado.EQUIPOS, SinCategorias, [], []));
        Assert.Equal(ResultadoAudiencia.AudienciaInvalida, Evaluar(Rol.PRESIDENTE, AlcanceComunicado.EQUIPOS, [C2014], [], [C2014]));
    }

    [Fact]
    public void Un_entrenador_a_todo_el_club_con_categorias_es_invalida_antes_que_fuera_de_su_alcance() =>
        Assert.Equal(
            ResultadoAudiencia.AudienciaInvalida,
            Evaluar(Rol.ENTRENADOR, AlcanceComunicado.CLUB, [C2014], [C2014], []));
}
