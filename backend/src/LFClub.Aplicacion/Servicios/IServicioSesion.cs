using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de la sesión (constitución §12.4).
/// Su responsabilidad es iniciar sesión con correo o documento, decir quién es la cuenta con
/// sesión, a qué clubes pertenece y entre cuáles de sus jugadores puede elegir en cada uno, y
/// renovar el token. Quien entra con el documento de un jugador recibe una sesión limitada a él:
/// no se le ofrecen ni se le entregan sus hermanos, y la limitación sigue al renovar (RF-026 de la
/// 006).
/// No registra cuentas, no cambia contraseñas y no decide el acceso a un club concreto ni quién
/// hace cada petición.
/// </summary>
public interface IServicioSesion
{
    /// <summary>
    /// Inicia sesión. Cuenta inexistente, contraseña incorrecta y cuenta bloqueada fallan con el
    /// mismo 401 <c>credenciales_invalidas</c>. Un documento que está en varias cuentas abre la del
    /// jugador activo (si no hay, la del que espera; si tampoco, la del retirado), y la contraseña
    /// de otra de esas cuentas recibe el mismo 401. Con un documento, el token queda limitado a los
    /// integrantes de la cuenta que tienen ese número; con el correo no tiene limitación.
    /// </summary>
    Task<TokenSesionDto> IniciarAsync(IniciarSesionDto datos, CancellationToken cancelacion = default);

    /// <summary>
    /// Devuelve la cuenta y sus clubes, uno por club; lista vacía para el DESARROLLADOR.
    /// <paramref name="limitacion"/> son los integrantes a los que está limitada la sesión, o nulo
    /// si llega a todos los de la cuenta.
    /// </summary>
    Task<SesionDto> ObtenerAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default);

    /// <summary>Cambia un token válido por uno nuevo, con la misma limitación.</summary>
    Task<TokenSesionDto> RenovarAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default);
}
