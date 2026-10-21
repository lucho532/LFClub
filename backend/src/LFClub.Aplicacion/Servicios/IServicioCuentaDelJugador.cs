using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta del estado de cuenta de un jugador (spec 015, RF-001 a RF-004).
/// Su responsabilidad es entregar sus cargos, sus pagos, su saldo y su estado, los medios de pago
/// activos del club y lo que quien pregunta puede hacer con ellos.
/// No cambia nada, no restringe ningún otro acceso aunque el jugador deba (§16.8) y no revela que
/// existe una cuenta ajena: lo que quien pregunta no puede ver responde 404.
/// </summary>
public interface IServicioCuentaDelJugador
{
    /// <summary>
    /// La cuenta de ese jugador, vista por <paramref name="quienPregunta"/>. El PRESIDENTE y el
    /// DIRECTIVO ven la de cualquier jugador del club, también retirado; la cuenta de un jugador solo la
    /// suya. Cualquier otro jugador, de otra familia o de otro club: 404.
    /// </summary>
    Task<CuentaJugadorDto> ObtenerAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
