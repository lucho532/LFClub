using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaCobroPlataformaPruebas
{
    [Fact]
    public void El_total_es_la_suma_de_integrantes_por_precio_de_cada_rol()
    {
        var calculo = ReglaCobroPlataforma.Calcular(
            new ConteoPorRol(20, 3, 2, 1),
            new PreciosPorRol(1000, 5000, 3000, 10000));

        Assert.NotNull(calculo);
        Assert.Equal((20 * 1000m) + (3 * 5000m) + (2 * 3000m) + 10000m, calculo.Total);
        Assert.Equal(5000m, calculo.PrecioEntrenador);
    }

    [Fact]
    public void Un_precio_sin_definir_cuenta_como_cero_si_hay_otro_definido()
    {
        var calculo = ReglaCobroPlataforma.Calcular(
            new ConteoPorRol(10, 4, 0, 1),
            new PreciosPorRol(2000, null, null, null));

        Assert.NotNull(calculo);
        Assert.Equal(20000m, calculo.Total);
        Assert.Equal(0m, calculo.PrecioEntrenador);
        Assert.Equal(0m, calculo.PrecioPresidente);
    }

    [Fact]
    public void Con_todos_los_precios_sin_definir_no_hay_cobro()
    {
        var calculo = ReglaCobroPlataforma.Calcular(
            new ConteoPorRol(10, 4, 2, 1), new PreciosPorRol(null, null, null, null));

        Assert.Null(calculo);
    }

    [Fact]
    public void Si_el_total_es_cero_no_hay_cobro()
    {
        Assert.Null(ReglaCobroPlataforma.Calcular(
            new ConteoPorRol(0, 0, 0, 0), new PreciosPorRol(1000, 1000, 1000, 1000)));
        Assert.Null(ReglaCobroPlataforma.Calcular(
            new ConteoPorRol(5, 1, 1, 1), new PreciosPorRol(0, 0, 0, 0)));
    }

    [Fact]
    public void Los_presidentes_a_precio_cero_no_suman()
    {
        var calculo = ReglaCobroPlataforma.Calcular(
            new ConteoPorRol(3, 0, 0, 2), new PreciosPorRol(1000, 0, 0, 0));

        Assert.NotNull(calculo);
        Assert.Equal(3000m, calculo.Total);
    }
}
