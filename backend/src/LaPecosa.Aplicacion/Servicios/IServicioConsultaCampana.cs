using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de consultar la campana de la cuenta de un jugador (spec 012, historia
/// 3).
/// Su responsabilidad es listar mezclados, del más reciente al más antiguo, los comunicados y los
/// avisos que le llegaron a la cuenta, y contar los que tiene sin leer, cada uno una sola vez.
/// No conoce HTTP, no cambia nada (marcar la lectura es de <see cref="IServicioLecturaCampana"/>) y
/// no revela de la actividad de un aviso más que su tipo, su identificador y si sigue disponible.
/// </summary>
public interface IServicioConsultaCampana
{
    /// <summary>
    /// Lo que le llegó a la cuenta, con el contador de sin leer. Solo la cuenta de un jugador:
    /// <c>403 rol_no_autorizado</c> a los demás roles.
    /// </summary>
    Task<PaginaDeCampanaDto> ListarAsync(
        int? limite,
        Guid? antesDe,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default);

    /// <summary>Cuántos elementos distintos tiene la cuenta sin leer. Solo la cuenta de un jugador.</summary>
    Task<ContadorDeCampanaDto> ContarSinLeerAsync(
        UsuarioRol quienPregunta, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default);
}
