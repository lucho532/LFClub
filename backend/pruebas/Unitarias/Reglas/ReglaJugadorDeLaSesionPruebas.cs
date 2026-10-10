using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaJugadorDeLaSesionPruebas
{
    private static readonly UsuarioRol Ana = new() { Nombres = "Ana" };
    private static readonly UsuarioRol Luis = new() { Nombres = "Luis" };
    private static readonly UsuarioRol Mara = new() { Nombres = "Mara" };
    private static readonly Guid Ajeno = Guid.NewGuid();

    // La tabla de research §2, fila por fila.
    [Fact]
    public void Sin_integrantes_en_el_club_no_se_encuentra_con_o_sin_jugador_elegido()
    {
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([], null, null));
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([], Ajeno, null));
    }

    [Fact]
    public void Con_un_integrante_y_sin_elegido_es_ese() =>
        Assert.Same(Ana, ReglaJugadorDeLaSesion.Resolver([Ana], null, null).Integrante);

    [Fact]
    public void Con_un_integrante_elegido_el_mismo_es_ese() =>
        Assert.Same(Ana, ReglaJugadorDeLaSesion.Resolver([Ana], Ana.Id, null).Integrante);

    [Fact]
    public void Con_un_integrante_y_elegido_otro_no_se_encuentra() =>
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([Ana], Ajeno, null));

    [Fact]
    public void Con_varios_y_elegido_uno_de_ellos_es_ese()
    {
        Assert.Same(Ana, ReglaJugadorDeLaSesion.Resolver([Ana, Luis, Mara], Ana.Id, null).Integrante);
        Assert.Same(Mara, ReglaJugadorDeLaSesion.Resolver([Ana, Luis, Mara], Mara.Id, null).Integrante);
    }

    [Fact]
    public void Con_varios_y_sin_elegido_hay_que_elegir()
    {
        var resultado = ReglaJugadorDeLaSesion.Resolver([Ana, Luis], null, null);

        Assert.True(resultado.SinElegir);
        Assert.Null(resultado.Integrante);
    }

    [Fact]
    public void Con_varios_y_elegido_otro_no_se_encuentra() =>
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([Ana, Luis], Ajeno, null));

    // La limitación de research §3: la sesión iniciada con el documento de un jugador.
    [Fact]
    public void La_limitacion_no_cambia_nada_con_un_solo_integrante()
    {
        // Ni cuando lo contiene ni cuando es de su integrante en otro club.
        Assert.Same(Ana, ReglaJugadorDeLaSesion.Resolver([Ana], null, [Ana.Id]).Integrante);
        Assert.Same(Ana, ReglaJugadorDeLaSesion.Resolver([Ana], null, [Ajeno]).Integrante);
        Assert.Same(Ana, ReglaJugadorDeLaSesion.Resolver([Ana], Ana.Id, [Ajeno]).Integrante);
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([Ana], Ajeno, [Ana.Id]));
    }

    [Fact]
    public void Con_varios_y_limitacion_es_el_de_la_limitacion_sin_tener_que_elegir()
    {
        var resultado = ReglaJugadorDeLaSesion.Resolver([Ana, Luis, Mara], null, [Luis.Id]);

        Assert.Same(Luis, resultado.Integrante);
        Assert.False(resultado.SinElegir);
    }

    [Fact]
    public void Con_varios_y_limitacion_elegir_al_mismo_es_ese() =>
        Assert.Same(Luis, ReglaJugadorDeLaSesion.Resolver([Ana, Luis], Luis.Id, [Luis.Id]).Integrante);

    [Fact]
    public void Con_varios_y_limitacion_el_elegido_no_puede_cambiarlo()
    {
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([Ana, Luis], Ana.Id, [Luis.Id]));
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([Ana, Luis], Ajeno, [Luis.Id]));
    }

    [Fact]
    public void Con_varios_y_ninguno_en_la_limitacion_no_se_encuentra()
    {
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([Ana, Luis], null, [Ajeno]));
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([Ana, Luis], Ana.Id, [Ajeno]));
        AssertNoEncontrado(ReglaJugadorDeLaSesion.Resolver([Ana, Luis], null, []));
    }

    [Fact]
    public void Con_limitacion_nunca_hay_que_elegir()
    {
        Guid?[] elegidos = [null, Ana.Id, Luis.Id, Ajeno];
        Guid[][] limitaciones = [[Ana.Id], [Luis.Id], [Ajeno], [Ana.Id, Ajeno]];

        foreach (var elegido in elegidos)
        {
            foreach (var limitacion in limitaciones)
            {
                Assert.False(ReglaJugadorDeLaSesion.Resolver([Ana, Luis], elegido, limitacion).SinElegir);
            }
        }
    }

    // Eso lo aplica después ReglaAccesoPorEstado.
    [Fact]
    public void No_mira_el_estado_de_ingreso_el_retiro_ni_el_rol()
    {
        var enEspera = new UsuarioRol { EstadoIngreso = EstadoIngreso.EN_ESPERA };
        var retirado = new UsuarioRol { Activo = false };
        var presidente = new UsuarioRol { Rol = Rol.PRESIDENTE };

        foreach (var integrante in new[] { enEspera, retirado, presidente })
        {
            Assert.Same(integrante, ReglaJugadorDeLaSesion.Resolver([integrante], null, null).Integrante);
            Assert.Same(integrante, ReglaJugadorDeLaSesion.Resolver([Ana, integrante], integrante.Id, null).Integrante);
            Assert.Same(integrante, ReglaJugadorDeLaSesion.Resolver([Ana, integrante], null, [integrante.Id]).Integrante);
        }
    }

    private static void AssertNoEncontrado(JugadorDeLaPeticion resultado)
    {
        Assert.Null(resultado.Integrante);
        Assert.False(resultado.SinElegir);
    }
}
