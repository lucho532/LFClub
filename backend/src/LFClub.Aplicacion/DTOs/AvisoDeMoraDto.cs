using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el aviso fijo de mora del inicio de la familia (spec 016, RF-006 a RF-010).
/// Su responsabilidad es llevar el estado del aviso (verde, ámbar o rojo) con lo que la pantalla
/// necesita decir: cuánto está vencido y de cuántas mensualidades, lo que está pendiente dentro del
/// plazo y hasta cuándo, y cuántos pagos hay en revisión.
/// No lleva cargos ni pagos, y estar en mora no restringe ningún acceso.
/// </summary>
/// <param name="Estado">Al día, pendiente dentro del plazo o en mora.</param>
/// <param name="ValorVencido">Pendiente de las mensualidades vencidas; 0 si no está en mora.</param>
/// <param name="MesesVencidos">Cantidad de mensualidades vencidas con algo pendiente.</param>
/// <param name="ValorEnPlazo">Pendiente de las mensualidades que aún no vencen; 0 si no hay.</param>
/// <param name="VenceEl">Día 5 del mes pendiente más cercano dentro del plazo; nulo si no hay.</param>
/// <param name="PagosEnRevision">Pagos del jugador en revisión; informativo, no cambia el estado.</param>
public record AvisoDeMoraDto(
    EstadoDeAvisoDeMora Estado,
    decimal ValorVencido,
    int MesesVencidos,
    decimal ValorEnPlazo,
    DateOnly? VenceEl,
    int PagosEnRevision);
