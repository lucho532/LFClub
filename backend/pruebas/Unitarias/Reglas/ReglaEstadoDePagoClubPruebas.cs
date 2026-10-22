using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Pruebas.Unitarias.Reglas;

public class ReglaEstadoDePagoClubPruebas
{
    private static readonly DateOnly Hoy = new(2026, 11, 6);
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private static EstadoDePagoClub Calcular(
        EstadoClub estado, MotivoSuspension? motivo, DateOnly? inicio, params CobroParaSuspension[] cobros) =>
        ReglaEstadoDePagoClub.Calcular(estado, motivo, inicio, cobros, Hoy);

    [Fact]
    public void Sin_mes_de_inicio_o_con_mes_futuro_es_sin_cobro()
    {
        Assert.Equal(EstadoDePagoClub.SIN_COBRO, Calcular(EstadoClub.ACTIVO, null, null));
        Assert.Equal(EstadoDePagoClub.SIN_COBRO, Calcular(EstadoClub.ACTIVO, null, new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void Con_mes_de_inicio_en_curso_y_sin_pendientes_esta_al_dia()
    {
        Assert.Equal(EstadoDePagoClub.AL_DIA, Calcular(EstadoClub.ACTIVO, null, Noviembre));
        Assert.Equal(
            EstadoDePagoClub.AL_DIA,
            Calcular(EstadoClub.ACTIVO, null, new DateOnly(2026, 6, 1), new CobroParaSuspension(EstadoCobroPlataforma.PAGADO, Hoy, false)));
    }

    [Fact]
    public void Un_cobro_pendiente_dentro_del_plazo_es_pendiente()
    {
        Assert.Equal(
            EstadoDePagoClub.PENDIENTE,
            Calcular(EstadoClub.ACTIVO, null, Noviembre, new CobroParaSuspension(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(1), false)));
    }

    [Fact]
    public void Un_cobro_pendiente_con_el_plazo_vencido_es_vencido_aunque_la_suspension_este_omitida()
    {
        Assert.Equal(
            EstadoDePagoClub.VENCIDO,
            Calcular(EstadoClub.ACTIVO, null, Noviembre, new CobroParaSuspension(EstadoCobroPlataforma.PENDIENTE, Hoy, false)));
        Assert.Equal(
            EstadoDePagoClub.VENCIDO,
            Calcular(EstadoClub.ACTIVO, null, Noviembre, new CobroParaSuspension(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(-3), true)));
    }

    [Fact]
    public void Suspendido_por_impago_se_distingue_del_suspendido_a_mano()
    {
        Assert.Equal(
            EstadoDePagoClub.SUSPENDIDO_POR_IMPAGO,
            Calcular(EstadoClub.SUSPENDIDO, MotivoSuspension.IMPAGO, Noviembre, new CobroParaSuspension(EstadoCobroPlataforma.PENDIENTE, Hoy.AddDays(-1), false)));
        Assert.Equal(
            EstadoDePagoClub.AL_DIA,
            Calcular(EstadoClub.SUSPENDIDO, MotivoSuspension.MANUAL, Noviembre));
    }
}
