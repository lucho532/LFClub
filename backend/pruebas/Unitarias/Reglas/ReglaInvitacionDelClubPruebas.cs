using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

/// <summary>Una invitación del club solo lleva JUGADOR, ENTRENADOR o DIRECTIVO (RF-001 y RF-002).</summary>
public class ReglaInvitacionDelClubPruebas
{
    [Theory]
    [InlineData(Rol.JUGADOR, true)]
    [InlineData(Rol.ENTRENADOR, true)]
    [InlineData(Rol.DIRECTIVO, true)]
    [InlineData(Rol.PRESIDENTE, false)]
    [InlineData(Rol.DESARROLLADOR, false)]
    public void Admite_solo_los_tres_roles_con_los_que_invita_el_club(Rol rol, bool esperado) =>
        Assert.Equal(esperado, ReglaInvitacionDelClub.Admite(rol));

    [Fact]
    public void Los_roles_invitables_son_exactamente_tres() =>
        Assert.Equal([Rol.JUGADOR, Rol.ENTRENADOR, Rol.DIRECTIVO], ReglaInvitacionDelClub.RolesInvitables);
}
