using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los pagos de los jugadores, sus aplicaciones y sus soportes (spec 015).
/// Su responsabilidad es consultar la cuenta de un jugador, bloquear al jugador mientras se confirma un
/// pago, dar el siguiente número de recibo del club y guardar pagos con sus aplicaciones.
/// No decide quién puede qué ni cuánto cubre un pago: eso lo hacen los servicios y las reglas.
/// </summary>
public interface IRepositorioPagos
{
    /// <summary>
    /// Los pagos de un jugador de cualquier estado, del más reciente al más antiguo; no los sigue. Del
    /// club de la petición.
    /// </summary>
    Task<IReadOnlyList<Pago>> ListarDelJugadorAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>De esos pagos, los que tienen una imagen de soporte guardada.</summary>
    Task<IReadOnlySet<Guid>> IdsConSoporteAsync(
        IReadOnlyCollection<Guid> pagoIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Bloquea la fila del jugador (<c>SELECT … FOR UPDATE</c>) hasta que termine la transacción en
    /// curso, para que dos confirmaciones sobre el mismo jugador se ejecuten una detrás de otra y nunca
    /// cubran más de lo que vale un cargo (research §7). Devuelve si el jugador existe en ese club.
    /// </summary>
    Task<bool> BloquearJugadorAsync(Guid clubId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Da el siguiente número de recibo del club sumando uno a su contador
    /// (<c>UPDATE … SET "UltimoRecibo" = "UltimoRecibo" + 1</c>), que deja el club bloqueado hasta el final
    /// de la transacción: consecutivo por club, sin saltos mientras la transacción se confirme.
    /// </summary>
    Task<int> SiguienteReciboAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>Añade un pago nuevo, con sus aplicaciones, para guardarlo con la unidad de trabajo.</summary>
    void Agregar(Pago pago);

    /// <summary>Un pago del club de la petición con sus aplicaciones, o nulo; lo sigue para poder cambiarlo.</summary>
    Task<Pago?> ObtenerAsync(Guid pagoId, CancellationToken cancelacion = default);

    /// <summary>Un pago del club de la petición, o nulo; solo para leerlo, sin seguirlo.</summary>
    Task<Pago?> ObtenerSinSeguirAsync(Guid pagoId, CancellationToken cancelacion = default);

    /// <summary>Los pagos del club en revisión, del más antiguo al más reciente; no los sigue.</summary>
    Task<IReadOnlyList<Pago>> ListarEnRevisionAsync(CancellationToken cancelacion = default);

    /// <summary>Cuántos pagos del club están en revisión.</summary>
    Task<int> ContarEnRevisionAsync(CancellationToken cancelacion = default);

    /// <summary>Añade la imagen de soporte de un pago nuevo, para guardarla con la unidad de trabajo.</summary>
    void AgregarSoporte(SoportePago soporte);

    /// <summary>La imagen de soporte de un pago, o nula si no tiene; es la única consulta que lee sus bytes.</summary>
    Task<SoportePago?> ObtenerSoporteAsync(Guid pagoId, CancellationToken cancelacion = default);
}
