using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta del aviso de mora de un jugador (spec 016, RF-001 a RF-017).
/// Su responsabilidad es entregar a la cuenta del propio jugador si está en mora, pendiente dentro del
/// plazo o al día, calculado con la fecha de Colombia en el momento de preguntar.
/// No cambia nada, no restringe ningún acceso aunque el jugador esté en mora y no revela que existe un
/// jugador ajeno: lo que quien pregunta no puede ver responde 404.
/// </summary>
public interface IServicioAvisoDeMora
{
    /// <summary>
    /// El aviso de ese jugador, visto por <paramref name="quienPregunta"/>. Solo lo ve la cuenta del
    /// propio jugador y mientras no esté retirado; cualquier otro, de otra familia o de otro club: 404.
    /// </summary>
    Task<AvisoDeMoraDto> ObtenerAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
