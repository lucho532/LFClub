using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaUltimoPresidentePruebas
{
    [Fact]
    public void El_unico_presidente_del_club_no_puede_dejar_el_rol() =>
        Assert.False(ReglaUltimoPresidente.PuedeDejarElRol(Rol.PRESIDENTE, 1));

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Con_otro_presidente_registrado_si_puede(int presidentes) =>
        Assert.True(ReglaUltimoPresidente.PuedeDejarElRol(Rol.PRESIDENTE, presidentes));

    [Fact]
    public void Las_invitaciones_pendientes_no_cuentan_solo_los_registrados()
    {
        // Un club con un presidente registrado y varias invitaciones pendientes sigue teniendo uno.
        const int registrados = 1;

        Assert.False(ReglaUltimoPresidente.PuedeDejarElRol(Rol.PRESIDENTE, registrados));
    }

    [Fact]
    public void Vale_igual_para_el_presidente_que_intenta_quitarse_su_propio_rol()
    {
        // RF-020: la regla no depende de quién lo pide, solo de cuántos presidentes quedan.
        Assert.False(ReglaUltimoPresidente.PuedeDejarElRol(Rol.PRESIDENTE, 1));
        Assert.True(ReglaUltimoPresidente.PuedeDejarElRol(Rol.PRESIDENTE, 2));
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO, 0)]
    [InlineData(Rol.ENTRENADOR, 1)]
    [InlineData(Rol.JUGADOR, 1)]
    public void Quien_no_es_presidente_no_esta_sujeto_a_la_regla(Rol rol, int presidentes) =>
        Assert.True(ReglaUltimoPresidente.PuedeDejarElRol(rol, presidentes));
}
