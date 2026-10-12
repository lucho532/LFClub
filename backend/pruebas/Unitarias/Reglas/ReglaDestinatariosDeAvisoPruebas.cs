using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaDestinatariosDeAvisoPruebas
{
    [Theory]
    [InlineData(HechoDeActividad.ENTRENAMIENTO_PROGRAMADO)]
    [InlineData(HechoDeActividad.ENTRENAMIENTO_CAMBIADO)]
    [InlineData(HechoDeActividad.ENTRENAMIENTO_CANCELADO)]
    [InlineData(HechoDeActividad.ENTRENAMIENTO_REACTIVADO)]
    [InlineData(HechoDeActividad.ASISTENCIA_REGISTRADA)]
    public void Los_hechos_de_un_entrenamiento_van_a_las_categorias(HechoDeActividad hecho)
    {
        Assert.Equal(CoberturaDeAviso.CATEGORIAS, ReglaDestinatariosDeAviso.Evaluar(hecho));
    }

    [Theory]
    [InlineData(HechoDeActividad.PARTIDO_CAMBIADO)]
    [InlineData(HechoDeActividad.PARTIDO_CANCELADO)]
    [InlineData(HechoDeActividad.PARTIDO_REACTIVADO)]
    public void Un_partido_proximo_que_cambia_va_solo_a_los_convocados(HechoDeActividad hecho)
    {
        Assert.Equal(CoberturaDeAviso.CONVOCADOS, ReglaDestinatariosDeAviso.Evaluar(hecho, EstadoPartido.PROXIMO));
    }

    [Theory]
    [InlineData(HechoDeActividad.PARTIDO_CAMBIADO)]
    [InlineData(HechoDeActividad.PARTIDO_CANCELADO)]
    [InlineData(HechoDeActividad.PARTIDO_REACTIVADO)]
    public void Un_partido_jugado_que_cambia_va_a_las_categorias(HechoDeActividad hecho)
    {
        Assert.Equal(CoberturaDeAviso.CATEGORIAS, ReglaDestinatariosDeAviso.Evaluar(hecho, EstadoPartido.JUGADO));
    }

    [Fact]
    public void Un_partido_cancelado_que_se_reactiva_va_a_los_convocados()
    {
        var cobertura = ReglaDestinatariosDeAviso.Evaluar(HechoDeActividad.PARTIDO_REACTIVADO, EstadoPartido.CANCELADO);

        Assert.Equal(CoberturaDeAviso.CONVOCADOS, cobertura);
    }

    [Fact]
    public void Convocar_a_un_jugador_va_solo_a_los_aludidos()
    {
        var cobertura = ReglaDestinatariosDeAviso.Evaluar(HechoDeActividad.JUGADOR_CONVOCADO, EstadoPartido.PROXIMO);

        Assert.Equal(CoberturaDeAviso.ALUDIDOS, cobertura);
    }

    [Fact]
    public void El_resultado_va_a_las_categorias()
    {
        var cobertura = ReglaDestinatariosDeAviso.Evaluar(HechoDeActividad.RESULTADO_PUBLICADO, EstadoPartido.JUGADO);

        Assert.Equal(CoberturaDeAviso.CATEGORIAS, cobertura);
    }

    [Fact]
    public void Cada_uno_de_los_diez_hechos_tiene_una_cobertura()
    {
        foreach (var hecho in Enum.GetValues<HechoDeActividad>())
        {
            Assert.True(Enum.IsDefined(ReglaDestinatariosDeAviso.Evaluar(hecho, EstadoPartido.PROXIMO)));
        }

        Assert.Equal(10, Enum.GetValues<HechoDeActividad>().Length);
    }
}
