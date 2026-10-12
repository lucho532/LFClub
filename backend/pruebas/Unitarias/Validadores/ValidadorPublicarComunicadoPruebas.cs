using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Unitarias.Validadores;

public class ValidadorPublicarComunicadoPruebas
{
    private static PublicarComunicadoDto Datos(
        string titulo = "Cambio de horario",
        string texto = "El sábado entrenamos a las 9.",
        Guid? clave = null,
        AlcanceComunicado alcance = AlcanceComunicado.CLUB) =>
        new(clave ?? Guid.NewGuid(), titulo, texto, alcance, null, null);

    private static ExcepcionDeAplicacion Falla(PublicarComunicadoDto datos) =>
        Assert.Throws<ExcepcionDeAplicacion>(() => ValidadorPublicarComunicado.Validar(datos));

    [Fact]
    public void Acepta_los_datos_correctos_y_recorta_los_espacios()
    {
        var validos = ValidadorPublicarComunicado.Validar(Datos("  Hola  ", "  Texto  "));

        Assert.Equal("Hola", validos.Titulo);
        Assert.Equal("Texto", validos.Texto);
        Assert.Empty(validos.CategoriaIds);
        Assert.Empty(validos.EquipoIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void El_titulo_y_el_texto_vacios_o_de_solo_espacios_fallan(string vacio)
    {
        Assert.Contains("titulo", Falla(Datos(titulo: vacio)).Errores!.Keys);
        Assert.Contains("texto", Falla(Datos(texto: vacio)).Errores!.Keys);
    }

    [Fact]
    public void El_titulo_admite_100_caracteres_y_no_101()
    {
        Assert.Equal(100, ValidadorPublicarComunicado.Validar(Datos(titulo: new string('a', 100))).Titulo.Length);
        Assert.Contains("titulo", Falla(Datos(titulo: new string('a', 101))).Errores!.Keys);
    }

    [Fact]
    public void El_texto_admite_1000_caracteres_y_no_1001()
    {
        Assert.Equal(1000, ValidadorPublicarComunicado.Validar(Datos(texto: new string('a', 1000))).Texto.Length);
        Assert.Contains("texto", Falla(Datos(texto: new string('a', 1001))).Errores!.Keys);
    }

    [Fact]
    public void Los_espacios_a_los_lados_no_cuentan_para_el_limite() =>
        Assert.Equal(
            100,
            ValidadorPublicarComunicado.Validar(Datos(titulo: " " + new string('a', 100) + " ")).Titulo.Length);

    [Fact]
    public void La_clave_vacia_falla()
    {
        var error = Falla(Datos(clave: Guid.Empty));

        Assert.Equal("datos_invalidos", error.Codigo);
        Assert.Contains("clave", error.Errores!.Keys);
    }

    [Fact]
    public void Un_alcance_inventado_falla() =>
        Assert.Contains("alcance", Falla(Datos(alcance: (AlcanceComunicado)99)).Errores!.Keys);

    [Fact]
    public void Quita_las_categorias_y_los_equipos_repetidos()
    {
        var categoria = Guid.NewGuid();
        var equipo = Guid.NewGuid();
        var datos = Datos(alcance: AlcanceComunicado.CATEGORIAS) with
        {
            CategoriaIds = [categoria, categoria],
            EquipoIds = [equipo, equipo],
        };

        var validos = ValidadorPublicarComunicado.Validar(datos);

        Assert.Equal([categoria], validos.CategoriaIds);
        Assert.Equal([equipo], validos.EquipoIds);
    }

    [Fact]
    public void Reune_los_errores_de_todos_los_campos()
    {
        var error = Falla(Datos(titulo: "", texto: "", clave: Guid.Empty));

        Assert.Equal(["clave", "texto", "titulo"], error.Errores!.Keys.Order().ToArray());
    }
}
