using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaMesDeCobroPruebas
{
    private static DateTime Utc(int anio, int mes, int dia, int hora, int minuto = 0) =>
        new(anio, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

    [Fact]
    public void El_31_de_octubre_a_las_2330_en_Bogota_sigue_siendo_octubre_aunque_en_UTC_sea_noviembre()
    {
        var ahora = Utc(2026, 11, 1, 4, 30);

        Assert.Equal(new DateOnly(2026, 10, 1), ReglaMesDeCobro.MesEnCurso(ahora));
        Assert.Equal(new DateOnly(2026, 11, 1), ReglaMesDeCobro.PrimerMesDeCobro(ahora));
    }

    [Fact]
    public void El_primer_dia_del_mes_a_las_0000_en_Bogota_ya_es_el_mes_nuevo()
    {
        var ahora = Utc(2026, 11, 1, 5, 0);

        Assert.Equal(new DateOnly(2026, 11, 1), ReglaMesDeCobro.MesEnCurso(ahora));
    }

    [Fact]
    public void Un_instante_de_madrugada_en_UTC_pertenece_al_mes_anterior_en_Colombia()
    {
        var ahora = Utc(2026, 11, 1, 4, 59);

        Assert.Equal(new DateOnly(2026, 10, 1), ReglaMesDeCobro.MesEnCurso(ahora));
    }

    [Fact]
    public void En_diciembre_el_primer_mes_de_cobro_es_enero_del_ano_siguiente()
    {
        var ahora = Utc(2026, 12, 15, 12);

        Assert.Equal(new DateOnly(2026, 12, 1), ReglaMesDeCobro.MesEnCurso(ahora));
        Assert.Equal(new DateOnly(2027, 1, 1), ReglaMesDeCobro.PrimerMesDeCobro(ahora));
    }

    [Fact]
    public void El_31_de_diciembre_de_noche_en_Bogota_sigue_en_diciembre()
    {
        var ahora = Utc(2027, 1, 1, 3, 0);

        Assert.Equal(new DateOnly(2026, 12, 1), ReglaMesDeCobro.MesEnCurso(ahora));
        Assert.Equal(new DateOnly(2027, 1, 1), ReglaMesDeCobro.PrimerMesDeCobro(ahora));
    }
}
