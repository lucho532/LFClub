using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaMayoriaDeEdadPruebas
{
    private static readonly DateOnly Nacimiento = new(2008, 10, 7);

    [Fact]
    public void El_dia_antes_de_cumplir_18_es_menor() =>
        Assert.True(ReglaMayoriaDeEdad.EsMenorDeEdad(Nacimiento, new DateOnly(2026, 10, 6)));

    [Fact]
    public void El_dia_de_su_cumpleanos_18_ya_no_es_menor() =>
        Assert.False(ReglaMayoriaDeEdad.EsMenorDeEdad(Nacimiento, new DateOnly(2026, 10, 7)));

    [Fact]
    public void Un_nino_es_menor_y_un_adulto_no()
    {
        var hoy = new DateOnly(2026, 10, 7);

        Assert.True(ReglaMayoriaDeEdad.EsMenorDeEdad(new DateOnly(2016, 3, 1), hoy));
        Assert.True(ReglaMayoriaDeEdad.EsMenorDeEdad(hoy, hoy));
        Assert.False(ReglaMayoriaDeEdad.EsMenorDeEdad(new DateOnly(1988, 3, 15), hoy));
    }

    [Fact]
    public void Quien_nacio_un_29_de_febrero_cumple_18_el_1_de_marzo_de_un_ano_no_bisiesto()
    {
        var bisiesto = new DateOnly(2008, 2, 29);

        Assert.True(ReglaMayoriaDeEdad.EsMenorDeEdad(bisiesto, new DateOnly(2026, 2, 28)));
        Assert.False(ReglaMayoriaDeEdad.EsMenorDeEdad(bisiesto, new DateOnly(2026, 3, 1)));
    }
}
