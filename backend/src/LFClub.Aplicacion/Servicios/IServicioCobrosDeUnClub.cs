using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa lo que el DESARROLLADOR hace con los cobros de un club (spec 017, RF-021 a RF-023): ver su
/// historial, marcar uno como pagado por fuera de la pasarela y anular uno pendiente con un motivo.
/// Marcar o anular reevalúa la suspensión en la misma transacción: un club suspendido por impago al que
/// ya no le queda un cobro vencido vuelve a estar activo, y una suspensión manual nunca se toca.
/// No devuelve datos personales.
/// </summary>
public interface IServicioCobrosDeUnClub
{
    /// <summary>Los cobros del club, el más reciente primero; <c>404</c> si el club no existe.</summary>
    Task<IReadOnlyList<CobroPlataformaDto>> ListarAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>
    /// Marca el cobro como pagado a mano, con quién y cuándo. <c>404</c> si no es del club; <c>409</c>
    /// si no está pendiente.
    /// </summary>
    Task<CobroPlataformaDto> MarcarPagadoAsync(
        Guid clubId, Guid cobroId, Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>
    /// Anula el cobro pendiente con un motivo de 3 a 300 caracteres. <c>404</c> si no es del club;
    /// <c>409</c> si no está pendiente.
    /// </summary>
    Task<CobroPlataformaDto> AnularAsync(
        Guid clubId, Guid cobroId, Guid usuarioId, AnularCobroDto datos, CancellationToken cancelacion = default);
}
