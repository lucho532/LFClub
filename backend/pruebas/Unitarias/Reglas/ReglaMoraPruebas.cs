using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaMoraPruebas
{
    private static readonly DateOnly Octubre = new(2026, 10, 1);
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    // La hora de Colombia es UTC-5: las 12:00 UTC son las 07:00 de ese mismo día.
    private static DateTime Utc(int anio, int mes, int dia, int hora = 12, int minuto = 0) =>
        new(anio, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

    private static ReglaMora.CargoParaMora Mensualidad(DateOnly mes, decimal valor = 65000m, decimal aplicado = 0m) =>
        new(TipoCargo.MENSUALIDAD, mes, valor, aplicado);

    [Fact]
    public void Sin_cargos_esta_al_dia()
    {
        var mora = ReglaMora.Calcular([], Utc(2026, 11, 20));

        Assert.Equal(EstadoDeAvisoDeMora.AL_DIA, mora.Estado);
        Assert.Equal(0m, mora.ValorVencido);
        Assert.Equal(0, mora.MesesVencidos);
        Assert.Null(mora.VenceEl);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void La_mensualidad_del_mes_sin_pagar_del_dia_1_al_5_esta_dentro_del_plazo(int dia)
    {
        var mora = ReglaMora.Calcular([Mensualidad(Noviembre)], Utc(2026, 11, dia));

        Assert.Equal(EstadoDeAvisoDeMora.PENDIENTE_EN_PLAZO, mora.Estado);
        Assert.Equal(65000m, mora.ValorEnPlazo);
        Assert.Equal(new DateOnly(2026, 11, 5), mora.VenceEl);
        Assert.Equal(0m, mora.ValorVencido);
    }

    [Fact]
    public void El_dia_6_sin_pagar_esta_en_mora()
    {
        var mora = ReglaMora.Calcular([Mensualidad(Noviembre)], Utc(2026, 11, 6));

        Assert.Equal(EstadoDeAvisoDeMora.EN_MORA, mora.Estado);
        Assert.Equal(65000m, mora.ValorVencido);
        Assert.Equal(1, mora.MesesVencidos);
    }

    [Fact]
    public void El_corte_es_a_las_0000_de_Colombia_no_a_las_0000_UTC()
    {
        var cargos = new[] { Mensualidad(Noviembre) };

        // 04:59 UTC del día 6 son las 23:59 del día 5 en Colombia; 05:00 UTC ya es el día 6.
        Assert.Equal(EstadoDeAvisoDeMora.PENDIENTE_EN_PLAZO, ReglaMora.Calcular(cargos, Utc(2026, 11, 6, 4, 59)).Estado);
        Assert.Equal(EstadoDeAvisoDeMora.EN_MORA, ReglaMora.Calcular(cargos, Utc(2026, 11, 6, 5, 0)).Estado);
    }

    [Fact]
    public void Una_mensualidad_de_un_mes_anterior_sin_pagar_es_mora_aunque_la_del_mes_este_pagada()
    {
        var cargos = new[] { Mensualidad(Octubre), Mensualidad(Noviembre, aplicado: 65000m) };

        var mora = ReglaMora.Calcular(cargos, Utc(2026, 11, 2));

        Assert.Equal(EstadoDeAvisoDeMora.EN_MORA, mora.Estado);
        Assert.Equal(65000m, mora.ValorVencido);
    }

    [Fact]
    public void La_mora_gana_a_lo_que_esta_dentro_del_plazo_pero_ambos_se_informan()
    {
        var cargos = new[] { Mensualidad(Octubre), Mensualidad(Noviembre) };

        var mora = ReglaMora.Calcular(cargos, Utc(2026, 11, 2));

        Assert.Equal(EstadoDeAvisoDeMora.EN_MORA, mora.Estado);
        Assert.Equal(65000m, mora.ValorVencido);
        Assert.Equal(65000m, mora.ValorEnPlazo);
        Assert.Equal(new DateOnly(2026, 11, 5), mora.VenceEl);
    }

    [Fact]
    public void Un_abono_parcial_deja_en_mora_solo_por_lo_que_falta()
    {
        var mora = ReglaMora.Calcular([Mensualidad(Octubre, aplicado: 25000m)], Utc(2026, 11, 20));

        Assert.Equal(EstadoDeAvisoDeMora.EN_MORA, mora.Estado);
        Assert.Equal(40000m, mora.ValorVencido);
    }

    [Fact]
    public void Varios_meses_vencidos_dan_un_solo_estado_con_el_total_y_la_cantidad()
    {
        var cargos = new[] { Mensualidad(new DateOnly(2026, 9, 1)), Mensualidad(Octubre, 70000m), Mensualidad(Noviembre) };

        var mora = ReglaMora.Calcular(cargos, Utc(2026, 11, 20));

        Assert.Equal(EstadoDeAvisoDeMora.EN_MORA, mora.Estado);
        Assert.Equal(200000m, mora.ValorVencido);
        Assert.Equal(3, mora.MesesVencidos);
    }

    [Fact]
    public void Lo_pagado_por_completo_o_de_mas_no_cuenta()
    {
        var cargos = new[] { Mensualidad(Octubre, aplicado: 65000m), Mensualidad(Noviembre, aplicado: 90000m) };

        Assert.Equal(EstadoDeAvisoDeMora.AL_DIA, ReglaMora.Calcular(cargos, Utc(2026, 11, 20)).Estado);
    }

    [Fact]
    public void Un_cargo_OTRO_sin_pagar_no_cuenta_para_la_mora()
    {
        var cargos = new[] { new ReglaMora.CargoParaMora(TipoCargo.OTRO, null, 30000m, 0m) };

        Assert.Equal(EstadoDeAvisoDeMora.AL_DIA, ReglaMora.Calcular(cargos, Utc(2026, 11, 20)).Estado);
    }

    [Fact]
    public void Diciembre_vence_el_5_de_diciembre_y_enero_el_5_de_enero_del_ano_siguiente()
    {
        var diciembre = new DateOnly(2026, 12, 1);

        Assert.Equal(EstadoDeAvisoDeMora.PENDIENTE_EN_PLAZO, ReglaMora.Calcular([Mensualidad(diciembre)], Utc(2026, 12, 5)).Estado);
        Assert.Equal(EstadoDeAvisoDeMora.EN_MORA, ReglaMora.Calcular([Mensualidad(diciembre)], Utc(2027, 1, 1)).Estado);
        Assert.Equal(new DateOnly(2027, 1, 5), ReglaMora.VencimientoDe(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void El_vencimiento_es_el_mas_cercano_de_lo_que_esta_dentro_del_plazo()
    {
        // Un cargo de un mes futuro (no debería existir, pero la regla no debe confundirse).
        var cargos = new[] { Mensualidad(new DateOnly(2026, 12, 1)), Mensualidad(Noviembre) };

        Assert.Equal(new DateOnly(2026, 11, 5), ReglaMora.Calcular(cargos, Utc(2026, 11, 2)).VenceEl);
    }
}
