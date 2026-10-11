using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaTransicionPartidoPruebas
{
    [Theory]
    [InlineData(EstadoPartido.PROXIMO, true)]
    [InlineData(EstadoPartido.CANCELADO, false)]
    [InlineData(EstadoPartido.JUGADO, false)]
    public void Solo_se_cancela_un_proximo(EstadoPartido estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionPartido.PuedeCancelar(estado));

    [Theory]
    [InlineData(EstadoPartido.PROXIMO, false)]
    [InlineData(EstadoPartido.CANCELADO, true)]
    [InlineData(EstadoPartido.JUGADO, false)]
    public void Solo_se_reactiva_un_cancelado(EstadoPartido estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionPartido.PuedeReactivar(estado));

    [Theory]
    [InlineData(EstadoPartido.PROXIMO, true)]
    [InlineData(EstadoPartido.CANCELADO, false)]
    [InlineData(EstadoPartido.JUGADO, true)]
    public void El_resultado_se_registra_en_un_proximo_y_se_corrige_en_un_jugado(EstadoPartido estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionPartido.PuedeRegistrarResultado(estado));

    [Theory]
    [InlineData(EstadoPartido.PROXIMO, true)]
    [InlineData(EstadoPartido.CANCELADO, false)]
    [InlineData(EstadoPartido.JUGADO, false)]
    public void Los_datos_solo_cambian_en_un_proximo(EstadoPartido estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionPartido.PuedeCambiarDatos(estado));

    [Theory]
    [InlineData(EstadoPartido.PROXIMO, true)]
    [InlineData(EstadoPartido.CANCELADO, false)]
    [InlineData(EstadoPartido.JUGADO, false)]
    public void La_convocatoria_solo_cambia_en_un_proximo(EstadoPartido estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionPartido.PuedeConvocar(estado));
}
