using LaPecosa.Aplicacion.Utilidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa la emisión de los tokens de sesión (research §4).
/// Su responsabilidad es firmar un token con el identificador del usuario y su sello de seguridad
/// y, cuando la sesión se inició con el documento de un jugador, con los integrantes a los que
/// queda limitada (research §3 de la 006).
/// No incluye en el token ni el club ni el rol, no decide a quién se limita una sesión y no valida
/// tokens recibidos.
/// </summary>
public interface IEmisorTokenSesion
{
    /// <summary>
    /// Emite un token para el usuario con su sello de seguridad actual. Con
    /// <paramref name="jugadores"/> la sesión queda limitada a esos integrantes; sin ellos, o con
    /// una colección vacía, llega a todos los de la cuenta.
    /// </summary>
    TokenEmitido Emitir(Guid usuarioId, Guid selloSeguridad, IReadOnlyCollection<Guid>? jugadores = null);
}
