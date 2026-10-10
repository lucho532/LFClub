using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaQuienRetiraPruebas
{
    private static UsuarioRol Integrante(Rol rol) => new() { Rol = rol, UsuarioId = Guid.NewGuid() };

    [Fact]
    public void El_presidente_retira_a_cualquiera_tambien_con_su_propio_identificador()
    {
        var presidente = Integrante(Rol.PRESIDENTE);

        Assert.True(ReglaQuienRetira.PuedeRetirar(presidente, Guid.NewGuid()));
        Assert.True(ReglaQuienRetira.PuedeRetirar(presidente, presidente.Id));
    }

    // RF-042 y RF-043: la familia solo retira al jugador de la petición.
    [Fact]
    public void La_cuenta_de_un_jugador_solo_retira_al_jugador_de_la_peticion()
    {
        var jugador = Integrante(Rol.JUGADOR);
        var hermano = new UsuarioRol { Rol = Rol.JUGADOR, UsuarioId = jugador.UsuarioId };

        Assert.True(ReglaQuienRetira.PuedeRetirar(jugador, jugador.Id));
        Assert.False(ReglaQuienRetira.PuedeRetirar(jugador, hermano.Id));
        Assert.False(ReglaQuienRetira.PuedeRetirar(jugador, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DESARROLLADOR)]
    public void Los_demas_roles_no_retiran_a_nadie_ni_a_si_mismos(Rol rol)
    {
        var integrante = Integrante(rol);

        Assert.False(ReglaQuienRetira.PuedeRetirar(integrante, Guid.NewGuid()));
        Assert.False(ReglaQuienRetira.PuedeRetirar(integrante, integrante.Id));
    }
}
