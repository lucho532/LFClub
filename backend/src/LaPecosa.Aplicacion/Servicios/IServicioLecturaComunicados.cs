using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de marcar un comunicado como leído (spec 011, historia 3).
/// Su responsabilidad es poner la lectura de la cuenta en todas las filas de sus jugadores para ese
/// comunicado, sin mover la primera lectura. "Marcar todos" es de <see cref="IServicioLecturaCampana"/>.
/// No conoce HTTP, no deja marcar lo que no le llegó a la cuenta y no guarda a qué hora ni desde
/// qué jugador se leyó más allá de la primera lectura.
/// </summary>
public interface IServicioLecturaComunicados
{
    /// <summary>
    /// Marca el comunicado como leído para toda la cuenta; es idempotente. <c>404</c> si no le llegó
    /// a la cuenta, está retirado o es de otro club.
    /// </summary>
    Task<ComunicadoRecibidoDto> MarcarLeidoAsync(
        Guid comunicadoId,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default);
}
