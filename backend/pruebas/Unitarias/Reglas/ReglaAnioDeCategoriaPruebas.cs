using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaAnioDeCategoriaPruebas
{
    private const int AnioEnCurso = 2026;

    [Theory]
    [InlineData(1000)]
    [InlineData(2012)]
    [InlineData(2020)]
    [InlineData(AnioEnCurso)]
    public void Un_anio_de_cuatro_cifras_no_posterior_al_anio_en_curso_es_valido(int anio) =>
        Assert.True(ReglaAnioDeCategoria.EsValido(anio, AnioEnCurso));

    [Theory]
    [InlineData(999)]
    [InlineData(14)]
    [InlineData(0)]
    [InlineData(-2014)]
    public void Un_anio_de_menos_de_cuatro_cifras_no_es_valido(int anio) =>
        Assert.False(ReglaAnioDeCategoria.EsValido(anio, AnioEnCurso));

    [Theory]
    [InlineData(AnioEnCurso + 1)]
    [InlineData(10000)]
    public void Un_anio_posterior_al_anio_en_curso_no_es_valido(int anio) =>
        Assert.False(ReglaAnioDeCategoria.EsValido(anio, AnioEnCurso));
}
