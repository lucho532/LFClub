using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso de "Eliminar mi cuenta" (spec 014; constitución §12.4 y §8).
/// Su responsabilidad es que una persona con sesión vea qué se iría con su cuenta y, con su
/// contraseña, la elimine con sus integrantes y jugadores en todos los clubes, en una sola
/// transacción, conservando lo publicado como "Persona eliminada".
/// Trabaja siempre sobre la cuenta de la sesión: no recibe identificador de cuenta ni de club. No
/// elimina la cuenta DESARROLLADOR y nunca deja a un club sin presidente.
/// </summary>
public interface IServicioEliminacionCuenta
{
    /// <summary>
    /// Todos los jugadores de la cuenta, de cualquier club y estado, y los clubes donde es el único
    /// PRESIDENTE. La cuenta DESARROLLADOR recibe 404 <c>no_encontrado</c>.
    /// </summary>
    Task<ResumenEliminacionCuentaDto> ObtenerResumenAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>
    /// Elimina la cuenta. 400 <c>datos_invalidos</c> sin contraseña; 403 <c>cuenta_bloqueada</c> si ya
    /// estaba bloqueada; 400 <c>contrasena_incorrecta</c> (suma un fallo de sesión) si no coincide;
    /// 409 <c>presidente_unico</c> con la lista de clubes si dejaría uno sin presidente; 404 para la
    /// cuenta DESARROLLADOR. En cualquier error no se elimina nada.
    /// </summary>
    Task EliminarAsync(Guid usuarioId, EliminarCuentaDto datos, CancellationToken cancelacion = default);
}
