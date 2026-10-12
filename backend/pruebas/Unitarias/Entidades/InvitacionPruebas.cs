using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Unitarias.Entidades;

public class InvitacionPruebas
{
    private static readonly DateTime Ahora = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    private static Invitacion Nueva(DateTime? usadaEn = null, DateTime? anuladaEn = null, DateTime? venceEn = null) => new()
    {
        CreadaEn = Ahora.AddDays(-1),
        VenceEn = venceEn ?? Ahora.AddDays(6),
        UsadaEn = usadaEn,
        AnuladaEn = anuladaEn,
    };

    [Fact]
    public void Sin_usar_sin_anular_y_antes_de_vencer_esta_pendiente() =>
        Assert.Equal(EstadoInvitacion.PENDIENTE, Nueva().EstadoEn(Ahora));

    [Fact]
    public void Con_fecha_de_uso_esta_usada() =>
        Assert.Equal(EstadoInvitacion.USADA, Nueva(usadaEn: Ahora.AddHours(-1)).EstadoEn(Ahora));

    [Fact]
    public void Con_fecha_de_anulacion_esta_cancelada() =>
        Assert.Equal(EstadoInvitacion.CANCELADA, Nueva(anuladaEn: Ahora.AddHours(-1)).EstadoEn(Ahora));

    [Fact]
    public void Pasado_su_vencimiento_esta_vencida() =>
        Assert.Equal(EstadoInvitacion.VENCIDA, Nueva(venceEn: Ahora.AddSeconds(-1)).EstadoEn(Ahora));

    [Fact]
    public void En_el_instante_exacto_de_su_vencimiento_ya_esta_vencida()
    {
        var invitacion = Nueva(venceEn: Ahora);

        Assert.Equal(EstadoInvitacion.VENCIDA, invitacion.EstadoEn(Ahora));
        Assert.Equal(EstadoInvitacion.PENDIENTE, invitacion.EstadoEn(Ahora.AddTicks(-1)));
        Assert.False(invitacion.EstaVigente(Ahora));
    }

    [Fact]
    public void Usada_gana_a_anulada_y_a_vencida()
    {
        var usadaYAnulada = Nueva(usadaEn: Ahora.AddHours(-2), anuladaEn: Ahora.AddHours(-1));
        var usadaYVencida = Nueva(usadaEn: Ahora.AddDays(-3), venceEn: Ahora.AddDays(-1));

        Assert.Equal(EstadoInvitacion.USADA, usadaYAnulada.EstadoEn(Ahora));
        Assert.Equal(EstadoInvitacion.USADA, usadaYVencida.EstadoEn(Ahora));
    }

    [Fact]
    public void Anulada_gana_a_vencida() =>
        Assert.Equal(
            EstadoInvitacion.CANCELADA,
            Nueva(anuladaEn: Ahora.AddDays(-2), venceEn: Ahora.AddDays(-1)).EstadoEn(Ahora));
}
