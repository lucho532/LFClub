using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaEnlaceDeFichaPruebas
{
    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    public void Un_autor_que_sigue_aprobado_con_un_rol_de_club_lleva_a_su_ficha_basica(Rol rol) =>
        Assert.Equal(TipoFicha.BASICA, ReglaEnlaceDeFicha.ParaAutor(rol, EstadoIngreso.APROBADO));

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public void Un_autor_en_espera_no_lleva_a_ninguna_ficha(Rol rol) =>
        Assert.Null(ReglaEnlaceDeFicha.ParaAutor(rol, EstadoIngreso.EN_ESPERA));

    [Fact]
    public void Un_autor_de_la_familia_o_retirado_no_lleva_a_ninguna_ficha() =>
        Assert.Null(ReglaEnlaceDeFicha.ParaAutor(Rol.JUGADOR, EstadoIngreso.APROBADO));

    [Fact]
    public void Un_autor_que_salio_del_club_rechazado_o_eliminado_no_lleva_a_ninguna_ficha() =>
        Assert.Null(ReglaEnlaceDeFicha.ParaAutor(null, null));

    [Fact]
    public void Una_persona_aprobada_que_es_jugador_activo_o_retirado_lleva_a_su_ficha_de_jugador() =>
        Assert.Equal(TipoFicha.JUGADOR, ReglaEnlaceDeFicha.ParaPersonaAprobada(Rol.JUGADOR, EstadoIngreso.APROBADO));

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    public void Una_persona_aprobada_que_hoy_tiene_un_rol_de_club_lleva_a_su_ficha_basica(Rol rol) =>
        Assert.Equal(TipoFicha.BASICA, ReglaEnlaceDeFicha.ParaPersonaAprobada(rol, EstadoIngreso.APROBADO));

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public void Una_persona_en_espera_no_lleva_a_ninguna_ficha(Rol rol) =>
        Assert.Null(ReglaEnlaceDeFicha.ParaPersonaAprobada(rol, EstadoIngreso.EN_ESPERA));

    [Fact]
    public void Una_persona_que_ya_no_existe_no_lleva_a_ninguna_ficha() =>
        Assert.Null(ReglaEnlaceDeFicha.ParaPersonaAprobada(null, null));

    [Fact]
    public void Un_hermano_jugador_aprobado_activo_o_retirado_lleva_a_su_ficha() =>
        Assert.Equal(TipoFicha.JUGADOR, ReglaEnlaceDeFicha.ParaHermano(Rol.JUGADOR, EstadoIngreso.APROBADO));

    [Theory]
    [InlineData(Rol.JUGADOR, EstadoIngreso.EN_ESPERA)]
    [InlineData(Rol.ENTRENADOR, EstadoIngreso.APROBADO)]
    [InlineData(Rol.PRESIDENTE, EstadoIngreso.APROBADO)]
    [InlineData(Rol.DIRECTIVO, EstadoIngreso.APROBADO)]
    public void Un_hermano_que_no_es_jugador_aprobado_no_lleva_a_ninguna_ficha(Rol rol, EstadoIngreso estado) =>
        Assert.Null(ReglaEnlaceDeFicha.ParaHermano(rol, estado));

    [Fact]
    public void Un_hermano_rechazado_o_que_ya_no_existe_no_lleva_a_ninguna_ficha() =>
        Assert.Null(ReglaEnlaceDeFicha.ParaHermano(null, null));

    [Fact]
    public void El_desarrollador_nunca_lleva_a_una_ficha()
    {
        Assert.Null(ReglaEnlaceDeFicha.ParaAutor(Rol.DESARROLLADOR, EstadoIngreso.APROBADO));
        Assert.Null(ReglaEnlaceDeFicha.ParaPersonaAprobada(Rol.DESARROLLADOR, EstadoIngreso.APROBADO));
        Assert.Null(ReglaEnlaceDeFicha.ParaHermano(Rol.DESARROLLADOR, EstadoIngreso.APROBADO));
    }
}
