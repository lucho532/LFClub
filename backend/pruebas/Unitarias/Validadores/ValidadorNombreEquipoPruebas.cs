using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;

namespace LFClub.Pruebas.Unitarias.Validadores;

public class ValidadorNombreEquipoPruebas
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_nombre_vacio_no_es_valido(string? nombre) => ComprobarQueSeRechaza(nombre);

    [Fact]
    public void Un_nombre_de_31_caracteres_no_es_valido() => ComprobarQueSeRechaza(new string('a', 31));

    [Fact]
    public void Un_nombre_de_30_caracteres_es_valido()
    {
        var nombre = new string('a', 30);

        Assert.Equal(nombre, ValidadorNombreEquipo.Validar(nombre).Nombre);
    }

    [Theory]
    [InlineData("A", "A", "a")]
    [InlineData("  Élite  ", "Élite", "élite")]
    [InlineData("Sub   12   B", "Sub 12 B", "sub 12 b")]
    public void Devuelve_el_nombre_sin_espacios_sobrantes_y_su_forma_en_minusculas(
        string escrito, string nombre, string normalizado)
    {
        var resultado = ValidadorNombreEquipo.Validar(escrito);

        Assert.Equal(nombre, resultado.Nombre);
        Assert.Equal(normalizado, resultado.Normalizado);
    }

    // Lo que decide si dos nombres son el mismo es la forma normalizada (RF-023).
    [Fact]
    public void Dos_nombres_que_solo_cambian_en_mayusculas_o_espacios_se_normalizan_igual() =>
        Assert.Equal(ValidadorNombreEquipo.Validar("Élite").Normalizado, ValidadorNombreEquipo.Validar(" éLITE ").Normalizado);

    private static void ComprobarQueSeRechaza(string? nombre)
    {
        var error = Assert.Throws<ExcepcionDeAplicacion>(() => ValidadorNombreEquipo.Validar(nombre));

        Assert.Equal("datos_invalidos", error.Codigo);
        Assert.Equal(400, error.EstadoHttp);
        Assert.True(error.Errores!.ContainsKey("nombre"));
    }
}
