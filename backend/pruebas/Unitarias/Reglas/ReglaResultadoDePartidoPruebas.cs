using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaResultadoDePartidoPruebas
{
    private static readonly Guid J3 = Guid.NewGuid();
    private static readonly Guid J4 = Guid.NewGuid();
    private static readonly Guid J5 = Guid.NewGuid();
    private static readonly Guid J9 = Guid.NewGuid();
    private static readonly HashSet<Guid> Convocados = [J3, J4, J5];

    private static ProblemaDeResultado? Validar(int club, int rival, params FilaDeResultado[] filas) =>
        ReglaResultadoDePartido.Validar(club, rival, Convocados, filas);

    [Fact]
    public void Tres_a_uno_con_dos_y_uno_es_valido() =>
        Assert.Null(Validar(3, 1, new FilaDeResultado(J3, true, 2), new FilaDeResultado(J4, true, 1), new FilaDeResultado(J5, false, 0)));

    [Fact]
    public void Cuatro_goles_de_jugadores_con_tres_del_club_se_rechaza() =>
        Assert.Equal(ProblemaDeResultado.GolesSuperanMarcador, Validar(3, 0, new FilaDeResultado(J3, true, 3), new FilaDeResultado(J4, true, 1)));

    [Fact]
    public void Un_gol_a_quien_no_jugo_se_rechaza() =>
        Assert.Equal(ProblemaDeResultado.GolesDeQuienNoJugo, Validar(3, 1, new FilaDeResultado(J3, false, 1)));

    [Fact]
    public void Dos_goles_de_jugadores_con_tres_del_club_se_acepta() =>
        Assert.Null(Validar(3, 2, new FilaDeResultado(J3, true, 1), new FilaDeResultado(J4, true, 1)));

    [Fact]
    public void Sin_filas_se_acepta() =>
        Assert.Null(Validar(0, 0));

    [Fact]
    public void Una_fila_de_quien_no_esta_convocado_se_rechaza() =>
        Assert.Equal(ProblemaDeResultado.JugadorNoConvocado, Validar(1, 0, new FilaDeResultado(J9, true, 1)));

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 100)]
    public void Goles_fuera_de_cero_a_noventa_y_nueve_se_rechazan(int club, int rival) =>
        Assert.Equal(ProblemaDeResultado.DatosFueraDeRango, Validar(club, rival));

    [Fact]
    public void Goles_negativos_de_un_jugador_o_una_fila_repetida_se_rechazan()
    {
        Assert.Equal(ProblemaDeResultado.DatosFueraDeRango, Validar(1, 0, new FilaDeResultado(J3, true, -1)));
        Assert.Equal(ProblemaDeResultado.DatosFueraDeRango, Validar(1, 0, new FilaDeResultado(J3, true, 1), new FilaDeResultado(J3, true, 0)));
    }
}
