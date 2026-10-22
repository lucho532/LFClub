using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>Una notificación de la pasarela con el club y el periodo del cobro asociado, si lo hay.</summary>
public record NotificacionConCobro(NotificacionPasarela Notificacion, Guid? ClubId, DateOnly? Periodo);

/// <summary>
/// Representa el acceso a los cobros de la plataforma, sus intentos, sus pagos y las notificaciones
/// de la pasarela (spec 017).
/// Su responsabilidad es leer y guardar esas cinco tablas, que no tienen filtro global de club
/// (research §6): cada consulta de club recibe el club y lo filtra de forma explícita.
/// No devuelve datos personales ni contiene reglas de negocio. Lo que se agrega se guarda con la
/// unidad de trabajo.
/// </summary>
public interface IRepositorioCobrosPlataforma
{
    /// <summary>Indica si el club ya tiene el cobro de ese periodo (primer día del mes).</summary>
    Task<bool> ExisteAsync(Guid clubId, DateOnly periodo, CancellationToken cancelacion = default);

    /// <summary>
    /// Clubes a los que ya les toca el cobro de ese periodo y todavía no lo tienen: activos o
    /// suspendidos (no dados de baja) con mes de inicio igual o anterior al periodo.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListarClubesSinCobroAsync(DateOnly periodo, CancellationToken cancelacion = default);

    /// <summary>Añade un cobro.</summary>
    void Agregar(CobroPlataforma cobro);

    /// <summary>Cobros del club, el más reciente primero.</summary>
    Task<IReadOnlyList<CobroPlataforma>> ListarDeClubAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>Un cobro del club; nulo si no existe o es de otro club.</summary>
    Task<CobroPlataforma?> ObtenerDeClubAsync(Guid clubId, Guid cobroId, CancellationToken cancelacion = default);

    /// <summary>Un cobro bloqueando su fila hasta el final de la transacción.</summary>
    Task<CobroPlataforma?> ObtenerBloqueandoAsync(Guid cobroId, CancellationToken cancelacion = default);

    /// <summary>Cobros pendientes del club, con seguimiento de cambios.</summary>
    Task<IReadOnlyList<CobroPlataforma>> ListarPendientesAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>Cobros pendientes de todos los clubes, agrupados por club, en una sola consulta.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<CobroPlataforma>>> ListarPendientesPorClubAsync(
        CancellationToken cancelacion = default);

    /// <summary>
    /// Clubes que el trabajo debe revisar: los que tienen un cobro pendiente y los suspendidos por
    /// impago.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListarClubesParaRevisarAsync(CancellationToken cancelacion = default);

    /// <summary>Pagos de esos cobros, por cobro.</summary>
    Task<IReadOnlyDictionary<Guid, PagoPlataforma>> ListarPagosAsync(
        IEnumerable<Guid> cobroIds, CancellationToken cancelacion = default);

    /// <summary>Indica si el cobro tiene un intento de pago creado desde ese instante.</summary>
    Task<bool> HayIntentoRecienteAsync(Guid cobroId, DateTime desdeUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si el último intento de pago del cobro ya recibió una notificación de la pasarela que
    /// dice que el pago no fue aprobado.
    /// </summary>
    Task<bool> UltimoIntentoNoAprobadoAsync(Guid cobroId, CancellationToken cancelacion = default);

    /// <summary>Añade un intento de pago.</summary>
    void AgregarIntento(IntentoPagoPlataforma intento);

    /// <summary>Busca un intento por su referencia.</summary>
    Task<IntentoPagoPlataforma?> ObtenerIntentoPorReferenciaAsync(
        string referencia, CancellationToken cancelacion = default);

    /// <summary>El pago del cobro; nulo si no tiene.</summary>
    Task<PagoPlataforma?> ObtenerPagoDeCobroAsync(Guid cobroId, CancellationToken cancelacion = default);

    /// <summary>Añade un pago.</summary>
    void AgregarPago(PagoPlataforma pago);

    /// <summary>Añade una notificación recibida.</summary>
    void AgregarNotificacion(NotificacionPasarela notificacion);

    /// <summary>Indica si esa transacción con ese estado ya se recibió.</summary>
    Task<bool> ExisteNotificacionAsync(
        string transaccionId, string estadoPasarela, CancellationToken cancelacion = default);

    /// <summary>Últimas notificaciones, la más reciente primero, filtrables por resultado.</summary>
    Task<IReadOnlyList<NotificacionConCobro>> ListarNotificacionesAsync(
        ResultadoNotificacion? resultado, int limite, CancellationToken cancelacion = default);
}
