using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Unitarias.Validadores;

public class ValidadorPagoPruebas
{
    private static readonly DateOnly Hoy = new(2026, 10, 20);

    private static RegistrarPagoDto Pago(
        decimal valor = 65000m, MedioPago medio = MedioPago.EFECTIVO, string? entidad = null, DateOnly? fecha = null) =>
        new(valor, medio, entidad, null, fecha ?? Hoy, null);

    private static ExcepcionDeAplicacion Falla(Action accion) => Assert.Throws<ExcepcionDeAplicacion>(accion);

    [Fact]
    public void Un_pago_en_efectivo_valido_pasa_y_descarta_la_entidad()
    {
        var datos = ValidadorPago.ValidarRegistro(Pago(entidad: "Nequi"), Hoy);

        Assert.Equal(65000m, datos.Valor);
        Assert.Null(datos.Entidad);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Cero_o_negativo_se_rechaza(int valor)
    {
        var error = Falla(() => ValidadorPago.ValidarRegistro(Pago(valor), Hoy));

        Assert.Equal("datos_invalidos", error.Codigo);
        Assert.Contains("valor", error.Errores!.Keys);
    }

    [Fact]
    public void Los_centavos_se_rechazan()
    {
        var error = Falla(() => ValidadorPago.ValidarRegistro(Pago(65000.50m), Hoy));

        Assert.Contains("valor", error.Errores!.Keys);
    }

    [Fact]
    public void Una_transferencia_exige_la_entidad()
    {
        var error = Falla(() => ValidadorPago.ValidarRegistro(Pago(medio: MedioPago.TRANSFERENCIA), Hoy));

        Assert.Contains("entidad", error.Errores!.Keys);
    }

    [Fact]
    public void Una_transferencia_con_entidad_pasa_y_la_limpia()
    {
        var datos = ValidadorPago.ValidarRegistro(Pago(medio: MedioPago.TRANSFERENCIA, entidad: "  Banco   Agrario "), Hoy);

        Assert.Equal("Banco Agrario", datos.Entidad);
    }

    [Fact]
    public void La_fecha_futura_se_rechaza_y_hoy_pasa()
    {
        Falla(() => ValidadorPago.ValidarRegistro(Pago(fecha: Hoy.AddDays(1)), Hoy));

        Assert.Equal(Hoy, ValidadorPago.ValidarRegistro(Pago(fecha: Hoy), Hoy).Fecha);
    }

    [Fact]
    public void La_referencia_tiene_hasta_60_caracteres()
    {
        var larga = Pago() with { Referencia = new string('9', 61) };

        var error = Falla(() => ValidadorPago.ValidarRegistro(larga, Hoy));

        Assert.Contains("referencia", error.Errores!.Keys);
        ValidadorPago.ValidarRegistro(Pago() with { Referencia = new string('9', 60) }, Hoy);
    }

    [Fact]
    public void Los_cargos_elegidos_se_devuelven_sin_repetir()
    {
        var id = Guid.NewGuid();

        var datos = ValidadorPago.ValidarRegistro(Pago() with { CargoIds = [id, id] }, Hoy);

        Assert.Equal([id], datos.CargoIds);
    }

    [Fact]
    public void Lo_informado_es_siempre_una_transferencia_y_exige_la_entidad()
    {
        var datos = ValidadorPago.ValidarInformado(new InformarPagoDto(65000m, "Nequi", null, Hoy), Hoy);

        Assert.Equal(MedioPago.TRANSFERENCIA, datos.Medio);
        Falla(() => ValidadorPago.ValidarInformado(new InformarPagoDto(65000m, " ", null, Hoy), Hoy));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void El_motivo_es_obligatorio(string? motivo) =>
        Falla(() => ValidadorPago.ValidarMotivo(new MotivoDto(motivo)));

    [Fact]
    public void El_motivo_tiene_hasta_300_caracteres_y_se_limpia()
    {
        Assert.Equal("Se equivocó", ValidadorPago.ValidarMotivo(new MotivoDto("  Se equivocó  ")));
        Falla(() => ValidadorPago.ValidarMotivo(new MotivoDto(new string('a', 301))));
    }

    [Fact]
    public void La_correccion_nula_pasa_y_la_invalida_no()
    {
        ValidadorPago.ValidarCorreccion(null);
        Falla(() => ValidadorPago.ValidarCorreccion(0m));
    }

    [Fact]
    public void El_valor_de_la_mensualidad_sigue_la_misma_regla()
    {
        ValidadorPago.ValidarValorDeMensualidad(65000m);
        Falla(() => ValidadorPago.ValidarValorDeMensualidad(0m));
        Falla(() => ValidadorPago.ValidarValorDeMensualidad(65000.5m));
    }
}
