using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaEliminacionDeCuentaPruebas
{
    private static readonly Guid ClubA = Guid.NewGuid();
    private static readonly Guid ClubB = Guid.NewGuid();

    private static UsuarioRol Integrante(Guid club, Rol rol) => new() { ClubId = club, Rol = rol };

    private static Dictionary<Guid, int> Presidentes(int enA, int enB = 0) =>
        new() { [ClubA] = enA, [ClubB] = enB };

    [Fact]
    public void El_presidente_unico_de_un_club_lo_bloquea() =>
        Assert.Equal(
            [ClubA],
            ReglaEliminacionDeCuenta.ClubesBloqueantes([Integrante(ClubA, Rol.PRESIDENTE)], Presidentes(1)));

    [Fact]
    public void Con_otro_presidente_en_el_club_no_bloquea() =>
        Assert.Empty(
            ReglaEliminacionDeCuenta.ClubesBloqueantes([Integrante(ClubA, Rol.PRESIDENTE)], Presidentes(2)));

    [Fact]
    public void Presidente_unico_en_un_club_y_no_en_otro_bloquea_solo_por_el_primero()
    {
        var integrantes = new[] { Integrante(ClubA, Rol.PRESIDENTE), Integrante(ClubB, Rol.PRESIDENTE) };

        Assert.Equal([ClubA], ReglaEliminacionDeCuenta.ClubesBloqueantes(integrantes, Presidentes(1, 2)));
    }

    [Fact]
    public void Presidente_unico_en_dos_clubes_los_devuelve_a_los_dos_sin_repetir()
    {
        var integrantes = new[]
        {
            Integrante(ClubA, Rol.PRESIDENTE), Integrante(ClubB, Rol.PRESIDENTE), Integrante(ClubA, Rol.PRESIDENTE),
        };

        Assert.Equal([ClubA, ClubB], ReglaEliminacionDeCuenta.ClubesBloqueantes(integrantes, Presidentes(1, 1)));
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public void Los_demas_roles_nunca_bloquean(Rol rol) =>
        Assert.Empty(ReglaEliminacionDeCuenta.ClubesBloqueantes([Integrante(ClubA, rol)], Presidentes(1)));

    [Fact]
    public void Una_cuenta_sin_integrantes_no_tiene_bloqueos() =>
        Assert.Empty(ReglaEliminacionDeCuenta.ClubesBloqueantes([], Presidentes(1)));

    [Fact]
    public void Un_club_sin_recuento_se_cuenta_como_de_presidente_unico() =>
        Assert.Equal(
            [ClubA],
            ReglaEliminacionDeCuenta.ClubesBloqueantes([Integrante(ClubA, Rol.PRESIDENTE)], new Dictionary<Guid, int>()));
}
