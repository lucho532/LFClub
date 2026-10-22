using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Base.SembradorCobrosPlataforma;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>
/// Ayudas comunes de las pruebas de la notificación de la pasarela (spec 017): un cobro con su intento,
/// el envío de una notificación firmada y la lectura de lo que quedó guardado.
/// </summary>
public abstract class NotificacionPasarelaBase
{
    protected readonly FabricaApi _fabrica;

    protected NotificacionPasarelaBase(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    protected async Task<(Club Club, CobroPlataforma Cobro, IntentoPagoPlataforma Intento)> EscenarioAsync(
        decimal total = 45000, EstadoCobroPlataforma estado = EstadoCobroPlataforma.PENDIENTE)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(
            club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8), total, estado);
        var intento = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);
        return (club, cobro, intento);
    }

    protected Task<HttpResponseMessage> EnviarAsync(string referencia, decimal valor, string estado = "APPROVED", string? transaccion = null)
    {
        var (cuerpo, checksum) = Notificacion(referencia, valor, estado, transaccion);
        return _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum);
    }

    protected Task<CobroPlataforma> CobroAsync(CobroPlataforma cobro) =>
        _fabrica.ConContextoAsync(contexto => contexto.CobrosPlataforma.AsNoTracking().SingleAsync(c => c.Id == cobro.Id));

    protected Task<List<PagoPlataforma>> PagosAsync(CobroPlataforma cobro) =>
        _fabrica.ConContextoAsync(contexto => contexto.PagosPlataforma.AsNoTracking().Where(p => p.CobroId == cobro.Id).ToListAsync());

    protected Task<List<NotificacionPasarela>> NotificacionesAsync(CobroPlataforma cobro) =>
        _fabrica.ConContextoAsync(contexto =>
            contexto.NotificacionesPasarela.AsNoTracking().Where(n => n.CobroId == cobro.Id).OrderBy(n => n.RecibidaEn).ToListAsync());
}
