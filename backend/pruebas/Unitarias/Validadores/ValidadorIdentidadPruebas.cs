using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Unitarias.Validadores;

/// <summary>
/// La identidad se valida igual en el registro y en la ficha del jugador: nombres y apellidos de
/// hasta 80 caracteres, documento de hasta 20 ya normalizado y fecha no futura (RF-021).
/// </summary>
public class ValidadorIdentidadPruebas
{
    private static readonly DateOnly Hoy = new(2026, 10, 9);

    private static string[] Rechazados(Action<ErroresDeValidacion> validar)
    {
        var errores = new ErroresDeValidacion();
        validar(errores);
        if (!errores.HayErrores)
        {
            return [];
        }

        var error = Assert.Throws<ExcepcionDeAplicacion>(errores.LanzarSiHayErrores);
        Assert.Equal("datos_invalidos", error.Codigo);
        return error.Errores!.Keys.Order().ToArray();
    }

    [Fact]
    public void Nombres_y_apellidos_admiten_80_caracteres_y_no_81()
    {
        Assert.Empty(Rechazados(e => ValidadorIdentidad.NombresYApellidos(e, new string('a', 80), new string('b', 80))));
        Assert.Equal(["nombres"], Rechazados(e => ValidadorIdentidad.NombresYApellidos(e, new string('a', 81), "Pérez")));
        Assert.Equal(["apellidos"], Rechazados(e => ValidadorIdentidad.NombresYApellidos(e, "Ana", new string('b', 81))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Nombres_y_apellidos_son_obligatorios(string? vacio) =>
        Assert.Equal(["apellidos", "nombres"], Rechazados(e => ValidadorIdentidad.NombresYApellidos(e, vacio, vacio)));

    [Fact]
    public void El_documento_admite_cada_tipo_de_la_lista()
    {
        foreach (var tipo in Enum.GetValues<TipoDocumento>())
        {
            Assert.Empty(Rechazados(e => ValidadorIdentidad.Documento(e, tipo, "1002003004")));
        }
    }

    [Fact]
    public void Un_tipo_de_documento_ausente_o_desconocido_se_rechaza()
    {
        Assert.Equal(["tipoDocumento"], Rechazados(e => ValidadorIdentidad.Documento(e, null, "1002003004")));
        Assert.Equal(["tipoDocumento"], Rechazados(e => ValidadorIdentidad.Documento(e, (TipoDocumento)99, "1002003004")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" . . ")]
    public void El_numero_de_documento_es_obligatorio(string? numero) =>
        Assert.Equal(["numeroDocumento"], Rechazados(e => ValidadorIdentidad.Documento(e, TipoDocumento.TARJETA_IDENTIDAD, numero)));

    // La longitud se mide sin los espacios ni los puntos, que no se guardan.
    [Fact]
    public void El_numero_admite_20_caracteres_ya_normalizado_y_no_21()
    {
        Assert.Empty(Rechazados(e => ValidadorIdentidad.Documento(e, TipoDocumento.CEDULA_CIUDADANIA, "1.234.567.890 1234567890")));
        Assert.Equal(
            ["numeroDocumento"],
            Rechazados(e => ValidadorIdentidad.Documento(e, TipoDocumento.CEDULA_CIUDADANIA, "123456789012345678901")));
    }

    [Fact]
    public void La_fecha_de_nacimiento_admite_hoy_y_rechaza_manana_y_la_ausente()
    {
        var errores = new ErroresDeValidacion();
        Assert.True(ValidadorIdentidad.FechaNacimiento(errores, Hoy, Hoy));
        Assert.True(ValidadorIdentidad.FechaNacimiento(errores, new DateOnly(2014, 7, 15), Hoy));
        Assert.False(errores.HayErrores);

        Assert.False(ValidadorIdentidad.FechaNacimiento(new ErroresDeValidacion(), Hoy.AddDays(1), Hoy));
        Assert.False(ValidadorIdentidad.FechaNacimiento(new ErroresDeValidacion(), null, Hoy));
        Assert.Equal(["fechaNacimiento"], Rechazados(e => ValidadorIdentidad.FechaNacimiento(e, Hoy.AddDays(1), Hoy)));
        Assert.Equal(["fechaNacimiento"], Rechazados(e => ValidadorIdentidad.FechaNacimiento(e, null, Hoy)));
    }
}
