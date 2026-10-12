using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaCorreccionDeIndicacionPruebas
{
    [Fact]
    public void Una_fila_de_la_familia_guardada_igual_sigue_siendo_de_la_familia() =>
        Assert.Equal(OrigenResultado.FAMILIA, ReglaCorreccionDeIndicacion.OrigenNuevo(OrigenResultado.FAMILIA, true, 1, true, 1));

    [Fact]
    public void Una_fila_sin_indicar_que_sigue_sin_jugar_sigue_sin_indicar() =>
        Assert.Null(ReglaCorreccionDeIndicacion.OrigenNuevo(null, false, 0, false, 0));

    [Fact]
    public void Una_fila_sin_indicar_marcada_como_que_jugo_pasa_a_gestion()
    {
        Assert.Equal(OrigenResultado.GESTION, ReglaCorreccionDeIndicacion.OrigenNuevo(null, false, 0, true, 0));
        Assert.Equal(OrigenResultado.GESTION, ReglaCorreccionDeIndicacion.OrigenNuevo(null, true, 0, true, 0));
    }

    [Fact]
    public void Una_fila_de_la_familia_con_goles_cambiados_pasa_a_gestion() =>
        Assert.Equal(OrigenResultado.GESTION, ReglaCorreccionDeIndicacion.OrigenNuevo(OrigenResultado.FAMILIA, true, 1, true, 2));

    [Fact]
    public void Una_fila_de_la_familia_marcada_como_que_no_jugo_pasa_a_gestion() =>
        Assert.Equal(OrigenResultado.GESTION, ReglaCorreccionDeIndicacion.OrigenNuevo(OrigenResultado.FAMILIA, true, 0, false, 0));

    [Fact]
    public void Una_fila_de_gestion_guardada_igual_sigue_de_gestion() =>
        Assert.Equal(OrigenResultado.GESTION, ReglaCorreccionDeIndicacion.OrigenNuevo(OrigenResultado.GESTION, false, 0, false, 0));
}
