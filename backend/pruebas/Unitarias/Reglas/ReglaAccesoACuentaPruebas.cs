using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaAccesoACuentaPruebas
{
    [Theory]
    [InlineData(Rol.PRESIDENTE, false, true, true)]
    [InlineData(Rol.PRESIDENTE, false, false, true)]
    [InlineData(Rol.DIRECTIVO, false, true, true)]
    [InlineData(Rol.DIRECTIVO, false, false, true)]
    [InlineData(Rol.JUGADOR, true, true, true)]
    [InlineData(Rol.JUGADOR, true, false, false)]
    [InlineData(Rol.JUGADOR, false, true, false)]
    [InlineData(Rol.ENTRENADOR, false, true, false)]
    [InlineData(Rol.ENTRENADOR, true, true, false)]
    [InlineData(Rol.DESARROLLADOR, false, true, false)]
    public void Ver_la_cuenta_depende_del_rol(Rol rol, bool esElMismo, bool activo, bool esperado) =>
        Assert.Equal(esperado, ReglaAccesoACuenta.PuedeVer(rol, esElMismo, activo));

    [Theory]
    [InlineData(Rol.PRESIDENTE, true)]
    [InlineData(Rol.DIRECTIVO, false)]
    [InlineData(Rol.ENTRENADOR, false)]
    [InlineData(Rol.JUGADOR, false)]
    [InlineData(Rol.DESARROLLADOR, false)]
    public void Solo_el_presidente_opera(Rol rol, bool esperado) =>
        Assert.Equal(esperado, ReglaAccesoACuenta.PuedeOperar(rol));

    [Theory]
    [InlineData(Rol.JUGADOR, true, true)]
    [InlineData(Rol.JUGADOR, false, false)]
    [InlineData(Rol.PRESIDENTE, true, false)]
    [InlineData(Rol.DIRECTIVO, true, false)]
    [InlineData(Rol.ENTRENADOR, true, false)]
    public void Solo_la_cuenta_del_propio_jugador_informa(Rol rol, bool esElMismo, bool esperado) =>
        Assert.Equal(esperado, ReglaAccesoACuenta.PuedeInformar(rol, esElMismo));

    [Theory]
    [InlineData(Rol.PRESIDENTE, true)]
    [InlineData(Rol.DIRECTIVO, true)]
    [InlineData(Rol.ENTRENADOR, false)]
    [InlineData(Rol.JUGADOR, false)]
    [InlineData(Rol.DESARROLLADOR, false)]
    public void Las_finanzas_las_ven_el_presidente_y_el_directivo(Rol rol, bool esperado) =>
        Assert.Equal(esperado, ReglaAccesoACuenta.VeFinanzasDelClub(rol));
}
