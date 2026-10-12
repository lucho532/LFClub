using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaAccesoPorEstadoPruebas
{
    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public void En_un_club_activo_entran_todos(Rol rol) =>
        Assert.Equal(ResultadoAcceso.Permitido, Aprobado(EstadoClub.ACTIVO, rol));

    [Fact]
    public void En_un_club_suspendido_entra_su_presidente() =>
        Assert.Equal(ResultadoAcceso.Permitido, Aprobado(EstadoClub.SUSPENDIDO, Rol.PRESIDENTE));

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public void En_un_club_suspendido_no_entra_nadie_mas(Rol rol) =>
        Assert.Equal(ResultadoAcceso.ClubSuspendido, Aprobado(EstadoClub.SUSPENDIDO, rol));

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public void En_un_club_dado_de_baja_no_entra_nadie(Rol rol) =>
        Assert.Equal(ResultadoAcceso.ClubDadoDeBaja, Aprobado(EstadoClub.DADO_DE_BAJA, rol));

    [Fact]
    public void En_un_club_activo_quien_esta_en_espera_no_entra() =>
        Assert.Equal(ResultadoAcceso.IngresoEnEspera, EnEspera(EstadoClub.ACTIVO));

    // Primero el estado del club y después el de ingreso (supuesto 2 de research.md).
    [Fact]
    public void En_un_club_suspendido_quien_esta_en_espera_recibe_el_motivo_del_club() =>
        Assert.Equal(ResultadoAcceso.ClubSuspendido, EnEspera(EstadoClub.SUSPENDIDO));

    [Fact]
    public void En_un_club_dado_de_baja_quien_esta_en_espera_recibe_el_motivo_del_club() =>
        Assert.Equal(ResultadoAcceso.ClubDadoDeBaja, EnEspera(EstadoClub.DADO_DE_BAJA));

    [Fact]
    public void En_un_club_activo_un_jugador_retirado_no_entra() =>
        Assert.Equal(ResultadoAcceso.IntegranteRetirado, Retirado(EstadoClub.ACTIVO));

    // Primero el estado del club, después el ingreso y después el retiro (supuesto 6 de la 003).
    [Fact]
    public void En_un_club_suspendido_un_jugador_retirado_recibe_el_motivo_del_club() =>
        Assert.Equal(ResultadoAcceso.ClubSuspendido, Retirado(EstadoClub.SUSPENDIDO));

    [Fact]
    public void En_un_club_dado_de_baja_un_jugador_retirado_recibe_el_motivo_del_club() =>
        Assert.Equal(ResultadoAcceso.ClubDadoDeBaja, Retirado(EstadoClub.DADO_DE_BAJA));

    [Fact]
    public void El_ingreso_en_espera_se_mira_antes_que_el_retiro() =>
        Assert.Equal(
            ResultadoAcceso.IngresoEnEspera,
            ReglaAccesoPorEstado.Evaluar(EstadoClub.ACTIVO, Rol.JUGADOR, EstadoIngreso.EN_ESPERA, activo: false));

    private static ResultadoAcceso Retirado(EstadoClub estado) =>
        ReglaAccesoPorEstado.Evaluar(estado, Rol.JUGADOR, EstadoIngreso.APROBADO, activo: false);

    private static ResultadoAcceso Aprobado(EstadoClub estado, Rol rol) =>
        ReglaAccesoPorEstado.Evaluar(estado, rol, EstadoIngreso.APROBADO, activo: true);

    private static ResultadoAcceso EnEspera(EstadoClub estado) =>
        ReglaAccesoPorEstado.Evaluar(estado, Rol.JUGADOR, EstadoIngreso.EN_ESPERA, activo: true);
}
