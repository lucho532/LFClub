using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaTransicionEstadoClubPruebas
{
    [Theory]
    [InlineData(EstadoClub.ACTIVO, EstadoClub.SUSPENDIDO)]
    [InlineData(EstadoClub.SUSPENDIDO, EstadoClub.ACTIVO)]
    [InlineData(EstadoClub.ACTIVO, EstadoClub.DADO_DE_BAJA)]
    [InlineData(EstadoClub.SUSPENDIDO, EstadoClub.DADO_DE_BAJA)]
    [InlineData(EstadoClub.DADO_DE_BAJA, EstadoClub.ACTIVO)]
    public void Permite_suspender_levantar_dar_de_baja_y_revertir_la_baja(EstadoClub desde, EstadoClub hacia) =>
        Assert.True(ReglaTransicionEstadoClub.SePermite(desde, hacia));

    [Theory]
    [InlineData(EstadoClub.ACTIVO, EstadoClub.ACTIVO)]
    [InlineData(EstadoClub.SUSPENDIDO, EstadoClub.SUSPENDIDO)]
    [InlineData(EstadoClub.DADO_DE_BAJA, EstadoClub.DADO_DE_BAJA)]
    [InlineData(EstadoClub.DADO_DE_BAJA, EstadoClub.SUSPENDIDO)]
    public void Rechaza_cualquier_otra_incluida_la_del_mismo_estado(EstadoClub desde, EstadoClub hacia) =>
        Assert.False(ReglaTransicionEstadoClub.SePermite(desde, hacia));

    [Fact]
    public void Las_nueve_combinaciones_estan_decididas_y_solo_cinco_se_permiten()
    {
        var estados = Enum.GetValues<EstadoClub>();
        var permitidas = estados.SelectMany(desde => estados.Select(hacia => (desde, hacia)))
            .Count(par => ReglaTransicionEstadoClub.SePermite(par.desde, par.hacia));

        Assert.Equal(3, estados.Length);
        Assert.Equal(5, permitidas);
    }

    [Theory]
    [InlineData(EstadoClub.ACTIVO, false)]
    [InlineData(EstadoClub.SUSPENDIDO, false)]
    [InlineData(EstadoClub.DADO_DE_BAJA, true)]
    public void Solo_se_puede_eliminar_un_club_dado_de_baja(EstadoClub estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionEstadoClub.SePuedeEliminar(estado));
}
