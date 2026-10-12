using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaEntrenadorAsignablePruebas
{
    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.PRESIDENTE)]
    public void Un_entrenador_un_directivo_y_un_presidente_aprobados_son_asignables(Rol rol) =>
        Assert.True(ReglaEntrenadorAsignable.EsAsignable(rol, EstadoIngreso.APROBADO));

    [Fact]
    public void Un_jugador_aprobado_no_es_asignable() =>
        Assert.False(ReglaEntrenadorAsignable.EsAsignable(Rol.JUGADOR, EstadoIngreso.APROBADO));

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.JUGADOR)]
    public void Quien_sigue_en_espera_no_es_asignable_con_ningun_rol(Rol rol) =>
        Assert.False(ReglaEntrenadorAsignable.EsAsignable(rol, EstadoIngreso.EN_ESPERA));

    [Fact]
    public void El_desarrollador_no_es_asignable() =>
        Assert.False(ReglaEntrenadorAsignable.EsAsignable(Rol.DESARROLLADOR, EstadoIngreso.APROBADO));
}
