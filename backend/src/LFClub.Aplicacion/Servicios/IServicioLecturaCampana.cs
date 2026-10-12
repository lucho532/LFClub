using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de marcar como leído lo que hay en la campana (spec 012, historia 3).
/// Su responsabilidad es poner la lectura de la cuenta en un aviso, sin mover la primera lectura, y
/// marcar todos los comunicados y avisos visibles de la cuenta.
/// No conoce HTTP, no deja marcar lo que no le llegó a la cuenta y no guarda desde qué jugador se
/// leyó. La lectura de un comunicado sigue en <see cref="IServicioLecturaComunicados"/>.
/// </summary>
public interface IServicioLecturaCampana
{
    /// <summary>
    /// Marca el aviso como leído para toda la cuenta; es idempotente. <c>404</c> si no le llegó a la
    /// cuenta o es de otro club.
    /// </summary>
    Task<AvisoRecibidoDto> MarcarAvisoLeidoAsync(
        Guid avisoId,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default);

    /// <summary>Marca como leídos todos los comunicados y avisos visibles de la cuenta y devuelve el contador.</summary>
    Task<ContadorDeCampanaDto> MarcarTodosAsync(
        UsuarioRol quienPregunta, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default);
}
