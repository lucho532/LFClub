using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Aplicacion.Validadores;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Pruebas.Unitarias.Validadores;

/// <summary>
/// Al agregar un hermano se exige su identidad, con las reglas del registro, y el responsable solo
/// si es menor y la cuenta no lo tiene (RF-002, RF-004 y RF-006 de la 006).
/// </summary>
public class ValidadorHermanoPruebas
{
    private static readonly DateOnly Hoy = new(2026, 10, 9);
    private static readonly DateOnly Menor = new(2016, 5, 20);
    private static readonly DateOnly Adulto = new(1988, 3, 15);

    private static AgregarHermanoDto Datos(
        string? nombres = "Luis",
        string? apellidos = "Gómez",
        TipoDocumento? tipo = TipoDocumento.TARJETA_IDENTIDAD,
        string? numero = "1002003004",
        DateOnly? fechaNacimiento = null,
        string? responsable = "Marta Gómez",
        bool sinFecha = false) =>
        new(nombres, apellidos, tipo, numero, sinFecha ? null : fechaNacimiento ?? Menor, responsable);

    private static string CampoRechazado(AgregarHermanoDto datos, bool cuentaTieneResponsable = false)
    {
        var error = Assert.Throws<ExcepcionDeAplicacion>(() => ValidadorHermano.Validar(datos, cuentaTieneResponsable, Hoy));

        Assert.Equal("datos_invalidos", error.Codigo);
        Assert.Equal(400, error.EstadoHttp);
        return Assert.Single(error.Errores!.Keys);
    }

    [Fact]
    public void Con_todos_los_datos_es_valido() => ValidadorHermano.Validar(Datos(), false, Hoy);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cada_dato_de_identidad_es_obligatorio(string? vacio)
    {
        Assert.Equal("nombres", CampoRechazado(Datos(nombres: vacio)));
        Assert.Equal("apellidos", CampoRechazado(Datos(apellidos: vacio)));
        Assert.Equal("numeroDocumento", CampoRechazado(Datos(numero: vacio)));
    }

    [Fact]
    public void El_tipo_de_documento_y_la_fecha_de_nacimiento_son_obligatorios()
    {
        Assert.Equal("tipoDocumento", CampoRechazado(Datos(tipo: null)));
        Assert.Equal("tipoDocumento", CampoRechazado(Datos(tipo: (TipoDocumento)99)));
        Assert.Equal("fechaNacimiento", CampoRechazado(Datos(sinFecha: true)));
    }

    [Fact]
    public void Sin_ningun_dato_se_rechaza_cada_campo_y_no_el_responsable()
    {
        var error = Assert.Throws<ExcepcionDeAplicacion>(
            () => ValidadorHermano.Validar(new AgregarHermanoDto(null, null, null, null, null, null), false, Hoy));

        Assert.Equal(
            ["apellidos", "fechaNacimiento", "nombres", "numeroDocumento", "tipoDocumento"],
            error.Errores!.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Los_nombres_y_los_apellidos_admiten_80_caracteres_y_no_81()
    {
        ValidadorHermano.Validar(Datos(nombres: new string('a', 80), apellidos: new string('b', 80)), false, Hoy);

        Assert.Equal("nombres", CampoRechazado(Datos(nombres: new string('a', 81))));
        Assert.Equal("apellidos", CampoRechazado(Datos(apellidos: new string('b', 81))));
    }

    // La longitud del documento se mide ya normalizado: los puntos y los espacios no cuentan.
    [Fact]
    public void El_numero_de_documento_admite_20_caracteres_y_no_21()
    {
        ValidadorHermano.Validar(Datos(numero: new string('1', 20)), false, Hoy);
        ValidadorHermano.Validar(Datos(numero: " 1.234.567.890 " + new string('1', 10)), false, Hoy);

        Assert.Equal("numeroDocumento", CampoRechazado(Datos(numero: new string('1', 21))));
    }

    [Fact]
    public void La_fecha_de_nacimiento_puede_ser_hoy_y_no_manana()
    {
        ValidadorHermano.Validar(Datos(fechaNacimiento: Hoy), false, Hoy);

        Assert.Equal("fechaNacimiento", CampoRechazado(Datos(fechaNacimiento: Hoy.AddDays(1))));
    }

    // RF-004.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_menor_sin_responsable_se_rechaza_si_la_cuenta_no_lo_tiene(string? responsable) =>
        Assert.Equal("nombreResponsable", CampoRechazado(Datos(responsable: responsable)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Un_menor_sin_responsable_es_valido_si_la_cuenta_ya_lo_tiene(string? responsable) =>
        ValidadorHermano.Validar(Datos(responsable: responsable), cuentaTieneResponsable: true, Hoy);

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Marta Gómez", false)]
    [InlineData(null, true)]
    public void Un_adulto_es_valido_con_o_sin_responsable(string? responsable, bool cuentaTieneResponsable) =>
        ValidadorHermano.Validar(Datos(fechaNacimiento: Adulto, responsable: responsable), cuentaTieneResponsable, Hoy);

    [Fact]
    public void Quien_cumple_18_hoy_ya_no_necesita_responsable_y_quien_los_cumple_manana_si()
    {
        ValidadorHermano.Validar(Datos(fechaNacimiento: new DateOnly(2008, 10, 9), responsable: null), false, Hoy);

        Assert.Equal(
            "nombreResponsable", CampoRechazado(Datos(fechaNacimiento: new DateOnly(2008, 10, 10), responsable: null)));
    }

    [Fact]
    public void El_responsable_que_se_va_a_guardar_admite_160_caracteres_y_no_161()
    {
        ValidadorHermano.Validar(Datos(responsable: new string('m', 160)), false, Hoy);

        Assert.Equal("nombreResponsable", CampoRechazado(Datos(responsable: new string('m', 161))));
    }

    // Supuesto 4: si no se va a guardar, ni se exige ni se valida.
    [Fact]
    public void El_responsable_que_se_ignora_no_se_valida()
    {
        ValidadorHermano.Validar(Datos(responsable: new string('m', 500)), cuentaTieneResponsable: true, Hoy);
        ValidadorHermano.Validar(Datos(fechaNacimiento: Adulto, responsable: new string('m', 500)), false, Hoy);
    }

    [Fact]
    public void Solo_se_guarda_el_responsable_de_un_menor_en_una_cuenta_que_no_lo_tiene()
    {
        Assert.True(ValidadorHermano.GuardaResponsable(Menor, cuentaTieneResponsable: false, Hoy));
        Assert.False(ValidadorHermano.GuardaResponsable(Menor, cuentaTieneResponsable: true, Hoy));
        Assert.False(ValidadorHermano.GuardaResponsable(Adulto, cuentaTieneResponsable: false, Hoy));
    }
}
