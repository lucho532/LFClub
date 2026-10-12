using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaMomentoDeActividadPruebas
{
    private static readonly DateOnly Hoy = new(2026, 10, 13);
    private static readonly TimeOnly CuatroDeLaTarde = new(16, 0);

    [Fact]
    public void Justo_a_la_hora_de_inicio_ya_empezo() =>
        Assert.True(ReglaMomentoDeActividad.YaEmpezo(Hoy, CuatroDeLaTarde, Hoy, CuatroDeLaTarde));

    [Fact]
    public void Un_minuto_antes_todavia_no_empieza() =>
        Assert.False(ReglaMomentoDeActividad.YaEmpezo(Hoy, CuatroDeLaTarde, Hoy, new TimeOnly(15, 59)));

    [Fact]
    public void Una_actividad_de_ayer_ya_empezo_aunque_hoy_sea_mas_temprano() =>
        Assert.True(ReglaMomentoDeActividad.YaEmpezo(Hoy.AddDays(-1), CuatroDeLaTarde, Hoy, new TimeOnly(6, 0)));

    [Fact]
    public void Una_actividad_de_manana_no_ha_empezado_aunque_hoy_sea_mas_tarde() =>
        Assert.False(ReglaMomentoDeActividad.YaEmpezo(Hoy.AddDays(1), new TimeOnly(8, 0), Hoy, new TimeOnly(22, 0)));

    [Fact]
    public void Misma_fecha_a_otra_hora_depende_de_la_hora()
    {
        Assert.True(ReglaMomentoDeActividad.YaEmpezo(Hoy, new TimeOnly(9, 0), Hoy, new TimeOnly(10, 0)));
        Assert.False(ReglaMomentoDeActividad.YaEmpezo(Hoy, new TimeOnly(11, 0), Hoy, new TimeOnly(10, 0)));
    }

    [Fact]
    public void Ya_termino_a_la_hora_de_fin_y_no_un_minuto_antes()
    {
        var fin = new TimeOnly(17, 30);

        Assert.True(ReglaMomentoDeActividad.YaTermino(Hoy, fin, Hoy, fin));
        Assert.False(ReglaMomentoDeActividad.YaTermino(Hoy, fin, Hoy, new TimeOnly(17, 29)));
    }

    [Fact]
    public void La_fecha_llega_desde_el_mismo_dia_y_no_antes()
    {
        Assert.True(ReglaMomentoDeActividad.SuFechaLlego(Hoy, Hoy));
        Assert.True(ReglaMomentoDeActividad.SuFechaLlego(Hoy.AddDays(-3), Hoy));
        Assert.False(ReglaMomentoDeActividad.SuFechaLlego(Hoy.AddDays(1), Hoy));
    }
}
