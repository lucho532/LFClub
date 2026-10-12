using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaAlcanceDeCategoriasPruebas
{
    [Theory]
    [InlineData(Rol.PRESIDENTE, true)]
    [InlineData(Rol.DIRECTIVO, true)]
    [InlineData(Rol.ENTRENADOR, false)]
    [InlineData(Rol.JUGADOR, false)]
    public void Solo_el_presidente_y_el_directivo_ven_todas_y_las_listas_del_club(Rol rol, bool esperado)
    {
        Assert.Equal(esperado, ReglaAlcanceDeCategorias.VeTodas(rol));
        Assert.Equal(esperado, ReglaAlcanceDeCategorias.VeListasDelClub(rol));
    }

    // Estar asignado no cambia nada para ellos (RF-017a): la ven activa o inactiva, con o sin asignación.
    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    public void El_presidente_y_el_directivo_ven_cualquier_categoria(Rol rol)
    {
        foreach (var activa in new[] { true, false })
        {
            foreach (var asignado in new[] { true, false })
            {
                Assert.True(ReglaAlcanceDeCategorias.PuedeVer(rol, activa, asignado));
            }
        }
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void El_entrenador_solo_ve_la_categoria_activa_que_tiene_asignada(bool activa, bool asignado, bool esperado) =>
        Assert.Equal(esperado, ReglaAlcanceDeCategorias.PuedeVer(Rol.ENTRENADOR, activa, asignado));

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void El_jugador_no_ve_ninguna_categoria_por_estos_endpoints(bool activa, bool asignado) =>
        Assert.False(ReglaAlcanceDeCategorias.PuedeVer(Rol.JUGADOR, activa, asignado));
}
