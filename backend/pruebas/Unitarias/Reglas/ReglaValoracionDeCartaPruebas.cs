using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaValoracionDeCartaPruebas
{
    // 435 / 6 = 72,5: la mitad redondea hacia arriba (CE-006).
    [Fact]
    public void Setenta_y_dos_y_medio_da_setenta_y_tres() =>
        Assert.Equal(73, ReglaValoracionDeCarta.Calcular(70, 65, 80, 72, 60, 88));

    [Fact]
    public void Todas_noventa_y_nueve_da_noventa_y_nueve() =>
        Assert.Equal(99, ReglaValoracionDeCarta.Calcular(99, 99, 99, 99, 99, 99));

    [Fact]
    public void Por_debajo_de_la_mitad_redondea_hacia_abajo() =>
        Assert.Equal(1, ReglaValoracionDeCarta.Calcular(1, 1, 1, 1, 1, 3));

    [Fact]
    public void Sin_estadisticas_no_hay_valoracion() =>
        Assert.Null(ReglaValoracionDeCarta.Calcular(null, null, null, null, null, null));
}
