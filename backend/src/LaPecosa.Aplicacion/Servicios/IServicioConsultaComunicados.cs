using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de consultar los comunicados (spec 011, historias 1, 2 y 4).
/// Su responsabilidad es listar lo publicado para el equipo del club según su rol, y listar, contar
/// y abrir lo que le llegó a la cuenta de un jugador, con el contador exacto y sin la audiencia.
/// No conoce HTTP, no cambia nada (marcar la lectura es de <see cref="IServicioLecturaComunicados"/>)
/// y no revela a la familia quién más lo recibió ni cuántos (RF-032).
/// </summary>
public interface IServicioConsultaComunicados
{
    /// <summary>
    /// Lo publicado, del más reciente al más antiguo: todo el club para PRESIDENTE y DIRECTIVO, solo
    /// lo suyo para ENTRENADOR. <c>403 rol_no_autorizado</c> a los demás roles.
    /// </summary>
    Task<PaginaDePublicadosDto> ListarPublicadosAsync(
        int? limite, Guid? antesDe, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>
    /// Un comunicado recibido; no lo marca como leído. <c>404</c> si no le llegó a la cuenta, está
    /// retirado o es de otro club.
    /// </summary>
    Task<ComunicadoRecibidoDto> ObtenerRecibidoAsync(
        Guid comunicadoId,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default);
}
