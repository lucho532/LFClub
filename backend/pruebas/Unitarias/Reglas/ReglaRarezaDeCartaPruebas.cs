using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaRarezaDeCartaPruebas
{
    [Fact]
    public void Sin_registros_es_bronce() =>
        Assert.Equal(RarezaCarta.BRONCE, ReglaRarezaDeCarta.Calcular(0, CalculoDeAsistencia.Porcentaje(0, 0)));

    [Theory]
    [InlineData(59, RarezaCarta.BRONCE)]
    [InlineData(60, RarezaCarta.PLATA)]
    [InlineData(84, RarezaCarta.PLATA)]
    [InlineData(85, RarezaCarta.ORO)]
    [InlineData(100, RarezaCarta.ORO)]
    public void Los_bordes(int porcentaje, RarezaCarta esperada) =>
        Assert.Equal(esperada, ReglaRarezaDeCarta.Calcular(10, porcentaje));

    // 169 de 200 es 84,5 %, que redondea a 85: se compara el porcentaje que ve la familia.
    [Fact]
    public void Ochenta_y_cuatro_y_medio_redondea_a_oro() =>
        Assert.Equal(RarezaCarta.ORO, ReglaRarezaDeCarta.Calcular(200, CalculoDeAsistencia.Porcentaje(169, 200)));
}
