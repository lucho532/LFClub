using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaVisibilidadDeComunicadosPublicadosPruebas
{
    private static readonly Guid Yo = Guid.NewGuid();
    private static readonly Guid Otro = Guid.NewGuid();

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    public void El_presidente_y_el_directivo_ven_todos_incluso_sin_autor(Rol rol)
    {
        Assert.True(ReglaVisibilidadDeComunicadosPublicados.Ve(rol, Yo, Yo));
        Assert.True(ReglaVisibilidadDeComunicadosPublicados.Ve(rol, Yo, Otro));
        Assert.True(ReglaVisibilidadDeComunicadosPublicados.Ve(rol, Yo, null));
    }

    [Fact]
    public void El_entrenador_ve_solo_los_suyos()
    {
        Assert.True(ReglaVisibilidadDeComunicadosPublicados.Ve(Rol.ENTRENADOR, Yo, Yo));
        Assert.False(ReglaVisibilidadDeComunicadosPublicados.Ve(Rol.ENTRENADOR, Yo, Otro));
        Assert.False(ReglaVisibilidadDeComunicadosPublicados.Ve(Rol.ENTRENADOR, Yo, null));
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.DESARROLLADOR)]
    public void El_jugador_y_el_desarrollador_no_ven_ninguno(Rol rol) =>
        Assert.False(ReglaVisibilidadDeComunicadosPublicados.Ve(rol, Yo, Yo));
}
