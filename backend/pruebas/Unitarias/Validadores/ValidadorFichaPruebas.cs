using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Unitarias.Validadores;

/// <summary>
/// El formulario de la ficha solo exige el celular y, si el jugador es menor de 18 años, el
/// responsable; todo lo demás es opcional (RF-002, RF-020).
/// </summary>
public class ValidadorFichaPruebas
{
    private static readonly ActualizarFichaDto SoloCelular = new(
        "3001234567", null, null, null, null, null, null, null, null, null, null, null);

    private static string Texto(int longitud) => new('a', longitud);

    private static string CampoRechazado(ActualizarFichaDto datos, bool menor = false)
    {
        var error = Assert.Throws<ExcepcionDeAplicacion>(() => ValidadorFicha.Validar(datos, menor));

        Assert.Equal("datos_invalidos", error.Codigo);
        Assert.Equal(400, error.EstadoHttp);
        return Assert.Single(error.Errores!.Keys);
    }

    [Fact]
    public void Todo_vacio_salvo_el_celular_es_valido_para_un_adulto() => ValidadorFicha.Validar(SoloCelular, false);

    [Fact]
    public void Los_textos_vacios_o_con_solo_espacios_son_validos() =>
        ValidadorFicha.Validar(
            new ActualizarFichaDto("3001234567", " ", "", " ", "", " ", "", null, "", " ", "", " "), false);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void El_celular_es_obligatorio(string? celular) =>
        Assert.Equal("celular", CampoRechazado(SoloCelular with { Celular = celular }));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void El_responsable_vacio_se_rechaza_con_un_menor_y_se_admite_con_un_adulto(string? responsable)
    {
        var datos = SoloCelular with { NombreResponsable = responsable };

        Assert.Equal("nombreResponsable", CampoRechazado(datos, menor: true));
        ValidadorFicha.Validar(datos, false);
    }

    [Fact]
    public void Un_menor_con_responsable_es_valido() =>
        ValidadorFicha.Validar(SoloCelular with { NombreResponsable = "Marta Gómez" }, true);

    // Cada máximo de data-model.md: en su límite es válido y con un carácter más se rechaza.
    [Theory]
    [InlineData("celular", 20)]
    [InlineData("nombreResponsable", 160)]
    [InlineData("emergenciaNombre", 160)]
    [InlineData("emergenciaParentesco", 40)]
    [InlineData("emergenciaCelular", 20)]
    [InlineData("entidadSalud", 120)]
    [InlineData("lugarAtencion", 200)]
    [InlineData("alergias", 1000)]
    [InlineData("enfermedades", 1000)]
    [InlineData("medicamentos", 1000)]
    [InlineData("observaciones", 1000)]
    public void Cada_texto_admite_su_maximo_y_no_un_caracter_mas(string campo, int maximo)
    {
        ValidadorFicha.Validar(Con(campo, Texto(maximo)), true);

        Assert.Equal(campo, CampoRechazado(Con(campo, Texto(maximo + 1)), menor: true));
    }

    [Fact]
    public void El_grupo_sanguineo_admite_los_valores_de_la_lista_y_ninguno()
    {
        foreach (var grupo in Enum.GetValues<GrupoSanguineo>())
        {
            ValidadorFicha.Validar(SoloCelular with { GrupoSanguineo = grupo }, false);
        }

        ValidadorFicha.Validar(SoloCelular with { GrupoSanguineo = null }, false);
    }

    [Fact]
    public void Un_grupo_sanguineo_desconocido_se_rechaza() =>
        Assert.Equal("grupoSanguineo", CampoRechazado(SoloCelular with { GrupoSanguineo = (GrupoSanguineo)99 }));

    /// <summary>Datos válidos de un menor, con ese campo sustituido.</summary>
    private static ActualizarFichaDto Con(string campo, string valor)
    {
        var datos = SoloCelular with { NombreResponsable = "Marta Gómez" };
        return campo switch
        {
            "celular" => datos with { Celular = valor },
            "nombreResponsable" => datos with { NombreResponsable = valor },
            "emergenciaNombre" => datos with { EmergenciaNombre = valor },
            "emergenciaParentesco" => datos with { EmergenciaParentesco = valor },
            "emergenciaCelular" => datos with { EmergenciaCelular = valor },
            "entidadSalud" => datos with { EntidadSalud = valor },
            "lugarAtencion" => datos with { LugarAtencion = valor },
            "alergias" => datos with { Alergias = valor },
            "enfermedades" => datos with { Enfermedades = valor },
            "medicamentos" => datos with { Medicamentos = valor },
            "observaciones" => datos with { Observaciones = valor },
            _ => throw new ArgumentOutOfRangeException(nameof(campo)),
        };
    }
}
