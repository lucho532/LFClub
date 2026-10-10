using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaAccesoAFichaPruebas
{
    private static readonly bool[] Ambos = [true, false];

    // Con categoría, sin ella o retirado, sea o no el de la petición y entrene o no la categoría.
    [Fact]
    public void El_presidente_ve_y_cambia_todo_de_cualquier_jugador()
    {
        foreach (var (suyo, activo, entrena) in Combinaciones())
        {
            Assert.Equal(
                new AlcanceDeFicha(true, true, true, true, true),
                ReglaAccesoAFicha.Evaluar(Rol.PRESIDENTE, suyo, activo, entrena));
        }
    }

    // RF-010: tampoco recibe los datos clínicos cuando está asignado como entrenador.
    [Fact]
    public void El_directivo_ve_cualquier_ficha_con_documentos_sin_datos_clinicos_y_sin_cambiar_nada()
    {
        foreach (var (suyo, activo, entrena) in Combinaciones())
        {
            Assert.Equal(
                new AlcanceDeFicha(true, false, true, false, false),
                ReglaAccesoAFicha.Evaluar(Rol.DIRECTIVO, suyo, activo, entrena));
        }
    }

    [Fact]
    public void El_entrenador_ve_con_datos_clinicos_y_sin_documentos_al_jugador_activo_de_su_categoria() =>
        Assert.Equal(
            new AlcanceDeFicha(true, true, false, false, false),
            ReglaAccesoAFicha.Evaluar(Rol.ENTRENADOR, esElJugadorDeLaPeticion: false, jugadorActivo: true, entrenaSuCategoria: true));

    // RF-008: otra categoría o sin categoría (no la entrena) y retirado (no está activo).
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void El_entrenador_no_ve_a_un_jugador_retirado_ni_al_de_una_categoria_que_no_entrena(
        bool jugadorActivo, bool entrenaSuCategoria)
    {
        foreach (var suyo in Ambos)
        {
            Assert.Equal(
                AlcanceDeFicha.Ninguno,
                ReglaAccesoAFicha.Evaluar(Rol.ENTRENADOR, suyo, jugadorActivo, entrenaSuCategoria));
        }
    }

    // RF-005, RF-016 y RF-017: todo menos corregir nombres, apellidos y fecha de nacimiento.
    [Fact]
    public void La_cuenta_del_jugador_ve_y_cambia_la_ficha_del_jugador_de_la_peticion_sin_corregir_la_identidad()
    {
        foreach (var activo in Ambos)
        {
            foreach (var entrena in Ambos)
            {
                Assert.Equal(
                    new AlcanceDeFicha(true, true, true, true, false),
                    ReglaAccesoAFicha.Evaluar(Rol.JUGADOR, esElJugadorDeLaPeticion: true, activo, entrena));
            }
        }
    }

    [Fact]
    public void La_cuenta_de_un_jugador_no_tiene_ningun_alcance_sobre_un_jugador_ajeno()
    {
        foreach (var activo in Ambos)
        {
            foreach (var entrena in Ambos)
            {
                Assert.Equal(
                    AlcanceDeFicha.Ninguno,
                    ReglaAccesoAFicha.Evaluar(Rol.JUGADOR, esElJugadorDeLaPeticion: false, activo, entrena));
            }
        }
    }

    [Fact]
    public void El_desarrollador_no_tiene_ningun_alcance()
    {
        foreach (var (suyo, activo, entrena) in Combinaciones())
        {
            Assert.Equal(AlcanceDeFicha.Ninguno, ReglaAccesoAFicha.Evaluar(Rol.DESARROLLADOR, suyo, activo, entrena));
        }
    }

    [Fact]
    public void Quien_no_ve_la_ficha_no_tiene_ningun_otro_indicador()
    {
        foreach (var rol in Enum.GetValues<Rol>())
        {
            foreach (var (suyo, activo, entrena) in Combinaciones())
            {
                var alcance = ReglaAccesoAFicha.Evaluar(rol, suyo, activo, entrena);
                if (!alcance.Ve)
                {
                    Assert.Equal(AlcanceDeFicha.Ninguno, alcance);
                }
            }
        }
    }

    private static IEnumerable<(bool Suyo, bool Activo, bool Entrena)> Combinaciones() =>
        from suyo in Ambos
        from activo in Ambos
        from entrena in Ambos
        select (suyo, activo, entrena);
}
