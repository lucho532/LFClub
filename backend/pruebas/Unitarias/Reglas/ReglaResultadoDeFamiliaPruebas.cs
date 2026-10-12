using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaResultadoDeFamiliaPruebas
{
    // Partido ya empezado sin marcador; la familia publica 2-1, jugó y un gol; nadie más marcó.
    private static ProblemaDeResultadoDeFamilia? Validar(
        EstadoPartido estado = EstadoPartido.PROXIMO,
        bool yaEmpezo = true,
        int? clubActual = null,
        int? rivalActual = null,
        int? clubEnviado = 2,
        int? rivalEnviado = 1,
        OrigenResultado? origen = null,
        bool jugo = true,
        int goles = 1,
        int demas = 0) =>
        ReglaResultadoDeFamilia.Validar(estado, yaEmpezo, clubActual, rivalActual, clubEnviado, rivalEnviado, origen, jugo, goles, demas);

    [Fact]
    public void Publicar_dos_a_uno_con_un_gol_es_valido() => Assert.Null(Validar());

    [Fact]
    public void Indicar_lo_suyo_con_marcador_publicado_es_valido() =>
        Assert.Null(Validar(EstadoPartido.JUGADO, clubActual: 2, rivalActual: 1, clubEnviado: null, rivalEnviado: null, demas: 1));

    [Fact]
    public void Un_partido_cancelado_se_rechaza() =>
        Assert.Equal(ProblemaDeResultadoDeFamilia.PartidoCancelado, Validar(EstadoPartido.CANCELADO));

    [Fact]
    public void Un_partido_que_no_empezo_se_rechaza() =>
        Assert.Equal(ProblemaDeResultadoDeFamilia.PartidoSinEmpezar, Validar(yaEmpezo: false));

    [Theory]
    [InlineData(null, null)]
    [InlineData(2, null)]
    [InlineData(null, 1)]
    public void Sin_marcador_y_sin_enviarlo_entero_falta_el_marcador(int? club, int? rival) =>
        Assert.Equal(ProblemaDeResultadoDeFamilia.FaltaMarcador, Validar(clubEnviado: club, rivalEnviado: rival));

    // Supuesto 3: también el mismo marcador.
    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    [InlineData(2, null)]
    public void Con_marcador_publicado_enviar_otro_se_rechaza_aunque_sea_igual(int? club, int? rival) =>
        Assert.Equal(
            ProblemaDeResultadoDeFamilia.MarcadorYaPublicado,
            Validar(EstadoPartido.JUGADO, clubActual: 2, rivalActual: 1, clubEnviado: club, rivalEnviado: rival));

    [Fact]
    public void Una_fila_corregida_por_quien_entrena_se_rechaza() =>
        Assert.Equal(
            ProblemaDeResultadoDeFamilia.IndicacionCorregida,
            Validar(EstadoPartido.JUGADO, clubActual: 2, rivalActual: 1, clubEnviado: null, rivalEnviado: null, origen: OrigenResultado.GESTION));

    [Fact]
    public void Una_fila_indicada_por_la_familia_se_puede_cambiar() =>
        Assert.Null(Validar(EstadoPartido.JUGADO, clubActual: 2, rivalActual: 1, clubEnviado: null, rivalEnviado: null, origen: OrigenResultado.FAMILIA, goles: 0));

    [Theory]
    [InlineData(100, 1, 0)]
    [InlineData(2, 100, 0)]
    [InlineData(-1, 1, 0)]
    [InlineData(2, 1, -1)]
    [InlineData(2, 1, 100)]
    public void Fuera_de_cero_a_noventa_y_nueve_se_rechaza(int club, int rival, int goles) =>
        Assert.Equal(ProblemaDeResultadoDeFamilia.DatosFueraDeRango, Validar(clubEnviado: club, rivalEnviado: rival, goles: goles));

    [Fact]
    public void Los_bordes_cero_y_noventa_y_nueve_se_aceptan()
    {
        Assert.Null(Validar(clubEnviado: 0, rivalEnviado: 0, jugo: false, goles: 0));
        Assert.Null(Validar(clubEnviado: 99, rivalEnviado: 99, goles: 99));
    }

    [Fact]
    public void Goles_de_quien_no_jugo_se_rechazan() =>
        Assert.Equal(ProblemaDeResultadoDeFamilia.GolesDeQuienNoJugo, Validar(jugo: false, goles: 1));

    [Fact]
    public void La_suma_exactamente_igual_al_marcador_es_valida() =>
        Assert.Null(Validar(EstadoPartido.JUGADO, clubActual: 2, rivalActual: 1, clubEnviado: null, rivalEnviado: null, goles: 1, demas: 1));

    [Fact]
    public void Un_gol_mas_que_el_marcador_se_rechaza()
    {
        Assert.Equal(
            ProblemaDeResultadoDeFamilia.GolesSuperanMarcador,
            Validar(EstadoPartido.JUGADO, clubActual: 2, rivalActual: 1, clubEnviado: null, rivalEnviado: null, goles: 2, demas: 1));
        Assert.Equal(ProblemaDeResultadoDeFamilia.GolesSuperanMarcador, Validar(clubEnviado: 2, rivalEnviado: 0, goles: 3));
    }

    // El orden de data-model: cada problema gana a los que van después.
    [Fact]
    public void Los_problemas_salen_en_su_orden()
    {
        Assert.Equal(ProblemaDeResultadoDeFamilia.PartidoCancelado, Validar(EstadoPartido.CANCELADO, yaEmpezo: false, clubEnviado: null));
        Assert.Equal(ProblemaDeResultadoDeFamilia.PartidoSinEmpezar, Validar(yaEmpezo: false, clubEnviado: null));
        Assert.Equal(ProblemaDeResultadoDeFamilia.FaltaMarcador, Validar(clubEnviado: null, jugo: false, goles: 5));
        Assert.Equal(
            ProblemaDeResultadoDeFamilia.MarcadorYaPublicado,
            Validar(EstadoPartido.JUGADO, clubActual: 2, rivalActual: 1, origen: OrigenResultado.GESTION));
        Assert.Equal(
            ProblemaDeResultadoDeFamilia.IndicacionCorregida,
            Validar(EstadoPartido.JUGADO, clubActual: 2, rivalActual: 1, clubEnviado: null, rivalEnviado: null, origen: OrigenResultado.GESTION, goles: 100));
        Assert.Equal(ProblemaDeResultadoDeFamilia.DatosFueraDeRango, Validar(jugo: false, goles: 100));
        Assert.Equal(ProblemaDeResultadoDeFamilia.GolesDeQuienNoJugo, Validar(clubEnviado: 0, jugo: false, goles: 1));
    }
}
