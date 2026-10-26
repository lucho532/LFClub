using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaCobroPlataformaPruebas
{
    private static readonly RangoTarifa[] Rangos =
    [
        new(1, 20, 25000),
        new(21, 40, 45000),
        new(41, 80, 75000),
        new(81, null, 110000),
    ];

    [Theory]
    [InlineData(1, 25000)]
    [InlineData(20, 25000)]
    [InlineData(21, 45000)]
    [InlineData(40, 45000)]
    [InlineData(41, 75000)]
    [InlineData(80, 75000)]
    [InlineData(81, 110000)]
    [InlineData(500, 110000)]
    public void El_total_es_el_precio_fijo_del_rango_que_cubre_los_jugadores(int jugadores, int esperado)
    {
        var calculo = ReglaCobroPlataforma.Calcular(new ConteoPorRol(jugadores, 3, 2, 1), Rangos);

        Assert.NotNull(calculo);
        Assert.Equal(esperado, calculo.Total);
        Assert.Equal(esperado, calculo.Rango.Precio);
    }

    [Fact]
    public void Los_demas_roles_no_cambian_el_total()
    {
        var pocos = ReglaCobroPlataforma.Calcular(new ConteoPorRol(10, 0, 0, 0), Rangos);
        var muchos = ReglaCobroPlataforma.Calcular(new ConteoPorRol(10, 30, 12, 4), Rangos);

        Assert.Equal(pocos!.Total, muchos!.Total);
    }

    [Fact]
    public void El_calculo_guarda_el_rango_aplicado_y_el_conteo()
    {
        var conteo = new ConteoPorRol(25, 3, 1, 1);

        var calculo = ReglaCobroPlataforma.Calcular(conteo, Rangos);

        Assert.Equal(new RangoTarifa(21, 40, 45000), calculo!.Rango);
        Assert.Equal(conteo, calculo.Conteo);
    }

    [Fact]
    public void Sin_rangos_no_hay_cobro()
    {
        Assert.Null(ReglaCobroPlataforma.Calcular(new ConteoPorRol(10, 4, 2, 1), []));
    }

    [Fact]
    public void Un_club_sin_jugadores_no_tiene_rango_y_no_hay_cobro()
    {
        Assert.Null(ReglaCobroPlataforma.BuscarRango(0, Rangos));
        Assert.Null(ReglaCobroPlataforma.Calcular(new ConteoPorRol(0, 2, 1, 1), Rangos));
    }

    [Fact]
    public void Si_el_precio_del_rango_es_cero_no_hay_cobro()
    {
        RangoTarifa[] gratis = [new(1, 10, 0), new(11, null, 20000)];

        Assert.Null(ReglaCobroPlataforma.Calcular(new ConteoPorRol(5, 1, 1, 1), gratis));
        Assert.NotNull(ReglaCobroPlataforma.Calcular(new ConteoPorRol(11, 1, 1, 1), gratis));
    }

    [Fact]
    public void Si_ningun_rango_cubre_la_cantidad_no_hay_cobro()
    {
        RangoTarifa[] corto = [new(1, 20, 25000)];

        Assert.Null(ReglaCobroPlataforma.Calcular(new ConteoPorRol(21, 1, 1, 1), corto));
    }
}
