using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using static LFClub.Dominio.Reglas.ReglaEstadoDeCuenta;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaEstadoDeCuentaPruebas
{
    [Fact]
    public void Sin_cargos_esta_al_dia()
    {
        var cuenta = Calcular([]);

        Assert.Equal(0m, cuenta.Saldo);
        Assert.Equal(EstadoDeCuenta.AL_DIA, cuenta.Estado);
    }

    [Fact]
    public void Un_cargo_sin_nada_aplicado_es_deuda()
    {
        var cuenta = Calcular([new ValorDeCargo(65000m, 0m)]);

        Assert.Equal(65000m, cuenta.Saldo);
        Assert.Equal(EstadoDeCuenta.DEBE, cuenta.Estado);
    }

    [Fact]
    public void Dos_cargos_de_65000_y_un_abono_de_90000_dejan_40000_y_pago_parcial()
    {
        var cuenta = Calcular([new ValorDeCargo(65000m, 65000m), new ValorDeCargo(65000m, 25000m)]);

        Assert.Equal(40000m, cuenta.Saldo);
        Assert.Equal(EstadoDeCuenta.PAGO_PARCIAL, cuenta.Estado);
    }

    [Fact]
    public void Todo_cubierto_esta_al_dia()
    {
        var cuenta = Calcular([new ValorDeCargo(65000m, 65000m), new ValorDeCargo(65000m, 65000m)]);

        Assert.Equal(0m, cuenta.Saldo);
        Assert.Equal(EstadoDeCuenta.AL_DIA, cuenta.Estado);
    }

    [Fact]
    public void Un_pago_anulado_no_suma_porque_quien_llama_no_lo_pasa()
    {
        // La anulación deja el cargo con aplicado 0: vuelve a deberse entero.
        var cuenta = Calcular([new ValorDeCargo(65000m, 0m)]);

        Assert.Equal(EstadoDeCuenta.DEBE, cuenta.Estado);
    }

    [Theory]
    [InlineData(65000, 0, 65000)]
    [InlineData(65000, 20000, 45000)]
    [InlineData(65000, 65000, 0)]
    [InlineData(65000, 70000, 0)]
    public void Lo_pendiente_de_un_cargo_nunca_es_negativo(int valor, int aplicado, int esperado) =>
        Assert.Equal(esperado, Pendiente(valor, aplicado));
}
