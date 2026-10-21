using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaTransicionPagoPruebas
{
    [Theory]
    [InlineData(EstadoPago.EN_REVISION, true)]
    [InlineData(EstadoPago.CONFIRMADO, false)]
    [InlineData(EstadoPago.RECHAZADO, false)]
    [InlineData(EstadoPago.ANULADO, false)]
    public void Solo_un_pago_en_revision_se_confirma_o_se_rechaza(EstadoPago estado, bool esperado)
    {
        Assert.Equal(esperado, ReglaTransicionPago.PuedeConfirmar(estado));
        Assert.Equal(esperado, ReglaTransicionPago.PuedeRechazar(estado));
    }

    [Theory]
    [InlineData(EstadoPago.EN_REVISION, false)]
    [InlineData(EstadoPago.CONFIRMADO, true)]
    [InlineData(EstadoPago.RECHAZADO, false)]
    [InlineData(EstadoPago.ANULADO, false)]
    public void Solo_un_pago_confirmado_se_anula(EstadoPago estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionPago.PuedeAnular(estado));

    [Theory]
    [InlineData(EstadoPago.EN_REVISION, false)]
    [InlineData(EstadoPago.CONFIRMADO, true)]
    [InlineData(EstadoPago.RECHAZADO, false)]
    [InlineData(EstadoPago.ANULADO, true)]
    public void Solo_el_confirmado_y_el_anulado_tienen_recibo(EstadoPago estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionPago.TieneRecibo(estado));

    [Theory]
    [InlineData(EstadoPago.EN_REVISION, false)]
    [InlineData(EstadoPago.CONFIRMADO, true)]
    [InlineData(EstadoPago.RECHAZADO, false)]
    [InlineData(EstadoPago.ANULADO, false)]
    public void Solo_el_confirmado_suma(EstadoPago estado, bool esperado) =>
        Assert.Equal(esperado, ReglaTransicionPago.Suma(estado));
}
