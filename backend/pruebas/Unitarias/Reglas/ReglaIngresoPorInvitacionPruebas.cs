using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaIngresoPorInvitacionPruebas
{
    [Theory]
    [InlineData(Rol.JUGADOR, true)]
    [InlineData(Rol.ENTRENADOR, true)]
    [InlineData(Rol.DIRECTIVO, true)]
    [InlineData(Rol.PRESIDENTE, false)]
    public void Es_del_club_toda_invitacion_que_no_es_de_presidente(Rol rol, bool esperado) =>
        Assert.Equal(esperado, ReglaIngresoPorInvitacion.EsDelClub(rol));

    [Theory]
    [InlineData(Rol.JUGADOR, true)]
    [InlineData(Rol.ENTRENADOR, false)]
    [InlineData(Rol.DIRECTIVO, false)]
    [InlineData(Rol.PRESIDENTE, false)]
    public void Solo_la_invitacion_de_jugador_pide_el_responsable(Rol rol, bool esperado) =>
        Assert.Equal(esperado, ReglaIngresoPorInvitacion.PideResponsable(rol));

    [Theory]
    [InlineData(Rol.JUGADOR, 2010, true)]
    [InlineData(Rol.JUGADOR, 2000, false)]
    [InlineData(Rol.ENTRENADOR, 2010, false)]
    [InlineData(Rol.DIRECTIVO, 2010, false)]
    [InlineData(Rol.PRESIDENTE, 2010, false)]
    public void Solo_el_jugador_menor_de_edad_debe_tener_responsable(Rol rol, int anioNacimiento, bool esperado) =>
        Assert.Equal(esperado, ReglaIngresoPorInvitacion.ExigeResponsable(
            rol, new DateOnly(anioNacimiento, 6, 1), new DateOnly(2026, 10, 9)));

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public void La_invitacion_del_club_no_sirve_mientras_el_club_esta_dado_de_baja(Rol rol)
    {
        Assert.True(ReglaIngresoPorInvitacion.ElClubPermiteUsarla(rol, EstadoClub.ACTIVO));
        Assert.True(ReglaIngresoPorInvitacion.ElClubPermiteUsarla(rol, EstadoClub.SUSPENDIDO));
        Assert.False(ReglaIngresoPorInvitacion.ElClubPermiteUsarla(rol, EstadoClub.DADO_DE_BAJA));
    }

    [Theory]
    [InlineData(EstadoClub.ACTIVO)]
    [InlineData(EstadoClub.SUSPENDIDO)]
    [InlineData(EstadoClub.DADO_DE_BAJA)]
    public void La_invitacion_de_presidente_no_depende_del_estado_del_club(EstadoClub estado) =>
        Assert.True(ReglaIngresoPorInvitacion.ElClubPermiteUsarla(Rol.PRESIDENTE, estado));
}
