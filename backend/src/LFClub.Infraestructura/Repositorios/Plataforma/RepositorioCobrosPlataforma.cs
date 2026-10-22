using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios.Plataforma;

/// <summary>
/// Representa el acceso a los cobros de la plataforma, sus intentos, sus pagos y las notificaciones
/// de la pasarela (spec 017).
/// Su responsabilidad es leer y guardar esas tablas, que no tienen filtro global de club
/// (research §6): toda consulta de un club filtra por <c>ClubId</c> de forma explícita.
/// No devuelve datos personales ni contiene reglas de negocio.
/// </summary>
public class RepositorioCobrosPlataforma : IRepositorioCobrosPlataforma
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioCobrosPlataforma(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(Guid clubId, DateOnly periodo, CancellationToken cancelacion = default) =>
        _contexto.CobrosPlataforma.AnyAsync(
            cobro => cobro.ClubId == clubId && cobro.Periodo == periodo, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListarClubesSinCobroAsync(
        DateOnly periodo, CancellationToken cancelacion = default) =>
        await _contexto.Clubes
            .Where(club => club.Estado != EstadoClub.DADO_DE_BAJA
                && club.MesInicioCobro != null
                && club.MesInicioCobro <= periodo
                && !_contexto.CobrosPlataforma.Any(cobro => cobro.ClubId == club.Id && cobro.Periodo == periodo))
            .Select(club => club.Id)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public void Agregar(CobroPlataforma cobro) => _contexto.CobrosPlataforma.Add(cobro);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CobroPlataforma>> ListarDeClubAsync(
        Guid clubId, CancellationToken cancelacion = default) =>
        await _contexto.CobrosPlataforma
            .Where(cobro => cobro.ClubId == clubId)
            .OrderByDescending(cobro => cobro.Periodo)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<CobroPlataforma?> ObtenerDeClubAsync(
        Guid clubId, Guid cobroId, CancellationToken cancelacion = default) =>
        _contexto.CobrosPlataforma.FirstOrDefaultAsync(
            cobro => cobro.ClubId == clubId && cobro.Id == cobroId, cancelacion);

    /// <inheritdoc />
    public Task<CobroPlataforma?> ObtenerBloqueandoAsync(Guid cobroId, CancellationToken cancelacion = default) =>
        _contexto.CobrosPlataforma
            .FromSqlInterpolated($"SELECT * FROM \"CobrosPlataforma\" WHERE \"Id\" = {cobroId} FOR UPDATE")
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CobroPlataforma>> ListarPendientesAsync(
        Guid clubId, CancellationToken cancelacion = default) =>
        await _contexto.CobrosPlataforma
            .Where(cobro => cobro.ClubId == clubId && cobro.Estado == EstadoCobroPlataforma.PENDIENTE)
            .OrderBy(cobro => cobro.Periodo)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<CobroPlataforma>>> ListarPendientesPorClubAsync(
        CancellationToken cancelacion = default)
    {
        var pendientes = await _contexto.CobrosPlataforma
            .AsNoTracking()
            .Where(cobro => cobro.Estado == EstadoCobroPlataforma.PENDIENTE)
            .ToListAsync(cancelacion);

        return pendientes
            .GroupBy(cobro => cobro.ClubId)
            .ToDictionary(grupo => grupo.Key, grupo => (IReadOnlyList<CobroPlataforma>)grupo.ToList());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListarClubesParaRevisarAsync(CancellationToken cancelacion = default)
    {
        var conCobro = _contexto.CobrosPlataforma
            .Where(cobro => cobro.Estado == EstadoCobroPlataforma.PENDIENTE)
            .Select(cobro => cobro.ClubId);
        var suspendidos = _contexto.Clubes
            .Where(club => club.Estado == EstadoClub.SUSPENDIDO && club.MotivoSuspension == MotivoSuspension.IMPAGO)
            .Select(club => club.Id);

        return await conCobro.Union(suspendidos).ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, PagoPlataforma>> ListarPagosAsync(
        IEnumerable<Guid> cobroIds, CancellationToken cancelacion = default)
    {
        var ids = cobroIds.ToList();
        return await _contexto.PagosPlataforma
            .AsNoTracking()
            .Where(pago => ids.Contains(pago.CobroId))
            .ToDictionaryAsync(pago => pago.CobroId, cancelacion);
    }

    /// <inheritdoc />
    public Task<bool> HayIntentoRecienteAsync(
        Guid cobroId, DateTime desdeUtc, CancellationToken cancelacion = default) =>
        _contexto.IntentosPagoPlataforma.AnyAsync(
            intento => intento.CobroId == cobroId && intento.CreadoEn >= desdeUtc, cancelacion);

    /// <inheritdoc />
    public async Task<bool> UltimoIntentoNoAprobadoAsync(Guid cobroId, CancellationToken cancelacion = default)
    {
        var referencia = await _contexto.IntentosPagoPlataforma
            .Where(intento => intento.CobroId == cobroId)
            .OrderByDescending(intento => intento.CreadoEn)
            .Select(intento => intento.Referencia)
            .FirstOrDefaultAsync(cancelacion);

        return referencia is not null
            && await _contexto.NotificacionesPasarela.AnyAsync(
                notificacion => notificacion.Referencia == referencia
                    && notificacion.Resultado == ResultadoNotificacion.NO_APROBADA,
                cancelacion);
    }

    /// <inheritdoc />
    public void AgregarIntento(IntentoPagoPlataforma intento) => _contexto.IntentosPagoPlataforma.Add(intento);

    /// <inheritdoc />
    public Task<IntentoPagoPlataforma?> ObtenerIntentoPorReferenciaAsync(
        string referencia, CancellationToken cancelacion = default) =>
        _contexto.IntentosPagoPlataforma.AsNoTracking()
            .FirstOrDefaultAsync(intento => intento.Referencia == referencia, cancelacion);

    /// <inheritdoc />
    public Task<PagoPlataforma?> ObtenerPagoDeCobroAsync(Guid cobroId, CancellationToken cancelacion = default) =>
        _contexto.PagosPlataforma.AsNoTracking().FirstOrDefaultAsync(pago => pago.CobroId == cobroId, cancelacion);

    /// <inheritdoc />
    public void AgregarPago(PagoPlataforma pago) => _contexto.PagosPlataforma.Add(pago);

    /// <inheritdoc />
    public void AgregarNotificacion(NotificacionPasarela notificacion) =>
        _contexto.NotificacionesPasarela.Add(notificacion);

    /// <inheritdoc />
    public Task<bool> ExisteNotificacionAsync(
        string transaccionId, string estadoPasarela, CancellationToken cancelacion = default) =>
        _contexto.NotificacionesPasarela.AnyAsync(
            notificacion => notificacion.TransaccionId == transaccionId
                && notificacion.EstadoPasarela == estadoPasarela,
            cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificacionConCobro>> ListarNotificacionesAsync(
        ResultadoNotificacion? resultado, int limite, CancellationToken cancelacion = default) =>
        await _contexto.NotificacionesPasarela
            .AsNoTracking()
            .Where(notificacion => resultado == null || notificacion.Resultado == resultado)
            .OrderByDescending(notificacion => notificacion.RecibidaEn)
            .Take(limite)
            .Select(notificacion => new NotificacionConCobro(
                notificacion,
                _contexto.CobrosPlataforma
                    .Where(cobro => cobro.Id == notificacion.CobroId)
                    .Select(cobro => (Guid?)cobro.ClubId)
                    .FirstOrDefault(),
                _contexto.CobrosPlataforma
                    .Where(cobro => cobro.Id == notificacion.CobroId)
                    .Select(cobro => (DateOnly?)cobro.Periodo)
                    .FirstOrDefault()))
            .ToListAsync(cancelacion);
}
