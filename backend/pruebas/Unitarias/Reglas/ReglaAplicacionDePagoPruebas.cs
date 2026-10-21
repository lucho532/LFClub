using LFClub.Dominio.Reglas;
using static LFClub.Dominio.Reglas.ReglaAplicacionDePago;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaAplicacionDePagoPruebas
{
    private static readonly Guid Agosto = Guid.NewGuid();
    private static readonly Guid Septiembre = Guid.NewGuid();
    private static readonly Guid Octubre = Guid.NewGuid();

    private static List<CargoPendiente> Tres() =>
    [
        new(Octubre, new DateOnly(2026, 10, 1), 65000m),
        new(Agosto, new DateOnly(2026, 8, 1), 65000m),
        new(Septiembre, new DateOnly(2026, 9, 1), 65000m),
    ];

    [Fact]
    public void Cubre_primero_el_cargo_mas_antiguo()
    {
        var reparto = Repartir(Tres(), 65000m);

        Assert.True(reparto.Aplicable);
        Assert.Equal([new Parte(Agosto, 65000m)], reparto.Partes);
    }

    [Fact]
    public void Un_abono_mayor_que_un_cargo_pasa_al_siguiente_mas_antiguo()
    {
        var reparto = Repartir(Tres(), 90000m);

        Assert.Equal([new Parte(Agosto, 65000m), new Parte(Septiembre, 25000m)], reparto.Partes);
    }

    [Fact]
    public void Un_abono_parcial_cubre_solo_una_parte_del_cargo()
    {
        var reparto = Repartir(Tres(), 20000m);

        Assert.Equal([new Parte(Agosto, 20000m)], reparto.Partes);
    }

    [Fact]
    public void Con_cargos_elegidos_usa_solo_esos()
    {
        var reparto = Repartir(Tres(), 65000m, [Octubre]);

        Assert.True(reparto.Aplicable);
        Assert.Equal([new Parte(Octubre, 65000m)], reparto.Partes);
    }

    [Fact]
    public void Con_cargos_elegidos_el_tope_es_lo_pendiente_de_esos()
    {
        var reparto = Repartir(Tres(), 100000m, [Octubre]);

        Assert.Equal(PagoExcedeDeuda, reparto.Rechazo);
        Assert.Equal(65000m, reparto.Debe);
    }

    [Fact]
    public void Un_pago_mayor_que_todo_lo_pendiente_se_rechaza_con_lo_que_se_debe()
    {
        var reparto = Repartir(Tres(), 195001m);

        Assert.False(reparto.Aplicable);
        Assert.Equal(PagoExcedeDeuda, reparto.Rechazo);
        Assert.Equal(195000m, reparto.Debe);
        Assert.Empty(reparto.Partes);
    }

    [Fact]
    public void El_pago_que_cubre_exactamente_todo_es_valido()
    {
        var reparto = Repartir(Tres(), 195000m);

        Assert.True(reparto.Aplicable);
        Assert.Equal(3, reparto.Partes.Count);
    }

    [Fact]
    public void Sin_nada_pendiente_no_hay_deuda()
    {
        var reparto = Repartir([new CargoPendiente(Agosto, new DateOnly(2026, 8, 1), 0m)], 1000m);

        Assert.Equal(SinDeuda, reparto.Rechazo);
    }

    [Fact]
    public void Los_cargos_elegidos_que_no_deben_nada_dejan_el_pago_sin_deuda()
    {
        var reparto = Repartir(Tres(), 1000m, [Guid.NewGuid()]);

        Assert.Equal(SinDeuda, reparto.Rechazo);
    }
}
