using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaEstadisticasDeCartaPruebas
{
    [Fact]
    public void Las_seis_nulas_son_validas_y_dejan_la_carta_sin_numeros() =>
        Assert.Null(ReglaEstadisticasDeCarta.Validar(null, null, null, null, null, null));

    [Fact]
    public void Las_seis_entre_uno_y_noventa_y_nueve_son_validas()
    {
        Assert.Null(ReglaEstadisticasDeCarta.Validar(70, 65, 80, 72, 60, 88));
        Assert.Null(ReglaEstadisticasDeCarta.Validar(1, 1, 1, 1, 1, 1));
        Assert.Null(ReglaEstadisticasDeCarta.Validar(99, 99, 99, 99, 99, 99));
    }

    [Fact]
    public void Cero_o_cien_se_rechazan_con_su_campo()
    {
        Assert.Equal("tiro", ReglaEstadisticasDeCarta.Validar(50, 0, 50, 50, 50, 50));
        Assert.Equal("fisico", ReglaEstadisticasDeCarta.Validar(50, 50, 50, 50, 50, 100));
    }

    [Fact]
    public void Cinco_de_seis_se_rechazan_con_el_que_falta() =>
        Assert.Equal("regate", ReglaEstadisticasDeCarta.Validar(50, 50, 50, null, 50, 50));
}
