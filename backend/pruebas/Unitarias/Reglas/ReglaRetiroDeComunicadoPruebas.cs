using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaRetiroDeComunicadoPruebas
{
    private static readonly Guid Yo = Guid.NewGuid();
    private static readonly Guid Otro = Guid.NewGuid();

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    public void El_autor_retira_el_suyo(Rol rol) =>
        Assert.Equal(ResultadoRetiroDeComunicado.Retira, ReglaRetiroDeComunicado.Evaluar(rol, Yo, Yo, false));

    [Fact]
    public void El_presidente_retira_cualquiera_tenga_autor_o_no()
    {
        Assert.Equal(ResultadoRetiroDeComunicado.Retira, ReglaRetiroDeComunicado.Evaluar(Rol.PRESIDENTE, Yo, Otro, false));
        Assert.Equal(ResultadoRetiroDeComunicado.Retira, ReglaRetiroDeComunicado.Evaluar(Rol.PRESIDENTE, Yo, null, false));
    }

    [Fact]
    public void El_directivo_que_no_es_el_autor_lo_ve_y_no_lo_retira()
    {
        Assert.Equal(ResultadoRetiroDeComunicado.NoRetira, ReglaRetiroDeComunicado.Evaluar(Rol.DIRECTIVO, Yo, Otro, false));
        Assert.Equal(ResultadoRetiroDeComunicado.NoRetira, ReglaRetiroDeComunicado.Evaluar(Rol.DIRECTIVO, Yo, null, false));
    }

    [Fact]
    public void El_entrenador_que_no_es_el_autor_no_lo_ve()
    {
        Assert.Equal(ResultadoRetiroDeComunicado.NoLoVe, ReglaRetiroDeComunicado.Evaluar(Rol.ENTRENADOR, Yo, Otro, false));
        Assert.Equal(ResultadoRetiroDeComunicado.NoLoVe, ReglaRetiroDeComunicado.Evaluar(Rol.ENTRENADOR, Yo, null, false));
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.DESARROLLADOR)]
    public void El_jugador_y_el_desarrollador_no_ven_comunicados_publicados(Rol rol) =>
        Assert.Equal(ResultadoRetiroDeComunicado.NoLoVe, ReglaRetiroDeComunicado.Evaluar(rol, Yo, Yo, false));

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    public void Uno_ya_retirado_no_se_retira_otra_vez(Rol rol) =>
        Assert.Equal(ResultadoRetiroDeComunicado.YaRetirado, ReglaRetiroDeComunicado.Evaluar(rol, Yo, Yo, true));

    [Fact]
    public void Quien_no_lo_ve_o_no_lo_retira_recibe_eso_antes_que_ya_retirado()
    {
        Assert.Equal(ResultadoRetiroDeComunicado.NoLoVe, ReglaRetiroDeComunicado.Evaluar(Rol.ENTRENADOR, Yo, Otro, true));
        Assert.Equal(ResultadoRetiroDeComunicado.NoRetira, ReglaRetiroDeComunicado.Evaluar(Rol.DIRECTIVO, Yo, Otro, true));
    }
}
