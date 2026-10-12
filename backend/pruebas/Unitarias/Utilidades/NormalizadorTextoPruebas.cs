using LFClub.Aplicacion.Utilidades;

namespace LFClub.Pruebas.Unitarias.Utilidades;

public class NormalizadorTextoPruebas
{
    [Theory]
    [InlineData("  Ana@Correo.COM ", "ana@correo.com")]
    [InlineData("ana@correo.com", "ana@correo.com")]
    [InlineData(null, "")]
    public void Correo_quita_espacios_de_los_extremos_y_pasa_a_minusculas(string? entrada, string esperado) =>
        Assert.Equal(esperado, NormalizadorTexto.Correo(entrada));

    [Theory]
    [InlineData("  Valfor   F.C. ", "valfor f.c.")]
    [InlineData("VALFOR F.C.", "valfor f.c.")]
    [InlineData("Valfor\tF.C.", "valfor f.c.")]
    public void NombreClub_pasa_a_minusculas_y_quita_espacios_sobrantes(string entrada, string esperado) =>
        Assert.Equal(esperado, NormalizadorTexto.NombreClub(entrada));

    [Fact]
    public void SinEspaciosSobrantes_conserva_las_mayusculas() =>
        Assert.Equal("Valfor F.C.", NormalizadorTexto.SinEspaciosSobrantes("  Valfor   F.C. "));

    [Theory]
    [InlineData(" 1.053.123 456 ", "1053123456")]
    [InlineData("AB 12.34", "ab1234")]
    [InlineData(null, "")]
    public void Documento_quita_espacios_y_puntos_y_pasa_a_minusculas(string? entrada, string esperado) =>
        Assert.Equal(esperado, NormalizadorTexto.Documento(entrada));

    [Theory]
    [InlineData("  Ana@Correo.com  ", "ana@correo.com")]
    [InlineData(" AB123 ", "ab123")]
    public void Identificador_quita_espacios_de_los_extremos_y_pasa_a_minusculas(string entrada, string esperado) =>
        Assert.Equal(esperado, NormalizadorTexto.Identificador(entrada));
}
