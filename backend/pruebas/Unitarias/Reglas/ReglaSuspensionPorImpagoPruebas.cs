using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaSuspensionPorImpagoPruebas
{
    private static readonly DateOnly Hoy = new(2026, 11, 6);

    private static CobroParaSuspension Cobro(
        EstadoCobroPlataforma estado, DateOnly suspendeDesde, bool omitida = false) =>
        new(estado, suspendeDesde, omitida);

    private static DecisionDeSuspension Evaluar(
        EstadoClub estado, MotivoSuspension? motivo, params CobroParaSuspension[] cobros) =>
        ReglaSuspensionPorImpago.Evaluar(estado, motivo, cobros, Hoy);

    [Fact]
    public void Un_club_activo_con_un_cobro_vencido_se_suspende()
    {
        Assert.Equal(
            DecisionDeSuspension.Suspender,
            Evaluar(EstadoClub.ACTIVO, null, Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(-1))));
    }

    [Fact]
    public void El_dia_de_suspension_ya_suspende_y_el_dia_anterior_todavia_no()
    {
        Assert.Equal(
            DecisionDeSuspension.Suspender,
            Evaluar(EstadoClub.ACTIVO, null, Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy)));
        Assert.Equal(
            DecisionDeSuspension.SinCambio,
            Evaluar(EstadoClub.ACTIVO, null, Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(1))));
    }

    [Fact]
    public void Un_cobro_pagado_o_anulado_no_suspende()
    {
        Assert.Equal(
            DecisionDeSuspension.SinCambio,
            Evaluar(
                EstadoClub.ACTIVO,
                null,
                Cobro(EstadoCobroPlataforma.PAGADO, Hoy.AddDays(-9)),
                Cobro(EstadoCobroPlataforma.ANULADO, Hoy.AddDays(-9))));
    }

    [Fact]
    public void Un_cobro_con_la_suspension_omitida_no_suspende()
    {
        Assert.Equal(
            DecisionDeSuspension.SinCambio,
            Evaluar(EstadoClub.ACTIVO, null, Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(-3), omitida: true)));
    }

    [Fact]
    public void Un_cobro_vencido_posterior_suspende_aunque_otro_tenga_la_suspension_omitida()
    {
        Assert.Equal(
            DecisionDeSuspension.Suspender,
            Evaluar(
                EstadoClub.ACTIVO,
                null,
                Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(-30), omitida: true),
                Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy)));
    }

    [Fact]
    public void Un_club_suspendido_por_impago_sin_cobros_vencidos_se_reactiva()
    {
        Assert.Equal(
            DecisionDeSuspension.Reactivar,
            Evaluar(EstadoClub.SUSPENDIDO, MotivoSuspension.IMPAGO, Cobro(EstadoCobroPlataforma.PAGADO, Hoy.AddDays(-2))));
        Assert.Equal(
            DecisionDeSuspension.Reactivar,
            Evaluar(EstadoClub.SUSPENDIDO, MotivoSuspension.IMPAGO));
    }

    [Fact]
    public void Con_dos_cobros_vencidos_pagar_uno_no_reactiva_y_pagar_los_dos_si()
    {
        Assert.Equal(
            DecisionDeSuspension.SinCambio,
            Evaluar(
                EstadoClub.SUSPENDIDO,
                MotivoSuspension.IMPAGO,
                Cobro(EstadoCobroPlataforma.PAGADO, Hoy.AddDays(-20)),
                Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(-2))));
        Assert.Equal(
            DecisionDeSuspension.Reactivar,
            Evaluar(
                EstadoClub.SUSPENDIDO,
                MotivoSuspension.IMPAGO,
                Cobro(EstadoCobroPlataforma.PAGADO, Hoy.AddDays(-20)),
                Cobro(EstadoCobroPlataforma.PAGADO, Hoy.AddDays(-2))));
    }

    [Fact]
    public void Una_suspension_manual_nunca_cambia()
    {
        Assert.Equal(DecisionDeSuspension.SinCambio, Evaluar(EstadoClub.SUSPENDIDO, MotivoSuspension.MANUAL));
        Assert.Equal(
            DecisionDeSuspension.SinCambio,
            Evaluar(EstadoClub.SUSPENDIDO, MotivoSuspension.MANUAL, Cobro(EstadoCobroPlataforma.PAGADO, Hoy)));
        Assert.Equal(DecisionDeSuspension.SinCambio, Evaluar(EstadoClub.SUSPENDIDO, null));
    }

    [Fact]
    public void Un_club_dado_de_baja_nunca_cambia()
    {
        Assert.Equal(
            DecisionDeSuspension.SinCambio,
            Evaluar(EstadoClub.DADO_DE_BAJA, null, Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(-5))));
    }

    [Fact]
    public void Un_cobro_con_la_suspension_omitida_deja_reactivar_un_club_suspendido_por_impago()
    {
        Assert.Equal(
            DecisionDeSuspension.Reactivar,
            Evaluar(
                EstadoClub.SUSPENDIDO,
                MotivoSuspension.IMPAGO,
                Cobro(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(-3), omitida: true)));
    }
}
