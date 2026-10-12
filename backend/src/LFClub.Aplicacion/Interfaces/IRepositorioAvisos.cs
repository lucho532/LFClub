using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los avisos de actividades del club de la petición (spec 012).
/// Su responsabilidad es resolver en una consulta del club los jugadores activos que reciben un aviso,
/// guardarlo con sus destinatarios reemplazando lo sin leer de la misma actividad, listar los avisos
/// que le llegaron a la cuenta de un jugador con cursor por identificador, contar los sin leer,
/// marcar la lectura de la cuenta y calcular si la actividad de cada aviso sigue disponible para
/// ella. Trabaja siempre con el filtro de aislamiento activo: nunca ve otro club.
/// No decide a quién llega cada hecho (eso es de las reglas del dominio), no abre transacciones, no
/// envía avisos fuera de la aplicación y solo borra por el reemplazo del sistema (RF-025).
/// </summary>
public interface IRepositorioAvisos
{
    /// <summary>
    /// Los jugadores activos del club (rol <c>JUGADOR</c>, ingreso aprobado, sin retirar y con
    /// categoría) que reciben un aviso con esa cobertura, en una sola consulta, sin las filas de la
    /// cuenta que hizo la acción (RF-005): <c>CATEGORIAS</c> los de esas categorías;
    /// <c>CONVOCADOS</c> los convocados al partido; <c>ALUDIDOS</c> solo esos jugadores.
    /// </summary>
    Task<IReadOnlyList<DestinatarioDeAviso>> ResolverDestinatariosAsync(
        CoberturaDeAviso cobertura,
        IReadOnlyCollection<Guid> categoriaIds,
        Guid? partidoId,
        IReadOnlyCollection<Guid> jugadorIds,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default);

    /// <summary>De esos integrantes, los jugadores activos, con su cuenta y sus nombres, para redactar.</summary>
    Task<IReadOnlyList<JugadorDeAviso>> JugadoresAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda el aviso con una fila por destinatario. Antes borra, para las cuentas destinatarias,
    /// sus filas sin leer de la misma actividad y los avisos que queden sin ningún destinatario
    /// (RF-025: una sola entrada sin leer por actividad); un aviso ya leído no se toca. Es un borrado
    /// del sistema, no de una acción de usuario (§14). Corre en la transacción del llamador.
    /// </summary>
    Task GenerarAsync(
        AvisoDeActividad aviso, IReadOnlyCollection<DestinatarioDeAviso> destinatarios, CancellationToken cancelacion = default);

    /// <summary>
    /// Los avisos que le llegaron a la cuenta, del más reciente al más antiguo, uno por aviso aunque
    /// llegue por varios hermanos. Solo cuentan filas de jugadores activos y, si la sesión está
    /// limitada, de esos jugadores. Devuelve hasta <paramref name="limite"/> filas, con el cursor
    /// <paramref name="antesDe"/>.
    /// </summary>
    Task<IReadOnlyList<AvisoDeCuenta>> ListarVisiblesAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid>? limitacion,
        Guid? antesDe,
        int limite,
        CancellationToken cancelacion = default);

    /// <summary>Cuántos avisos distintos tiene la cuenta sin leer, con las mismas condiciones.</summary>
    Task<int> ContarSinLeerAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default);

    /// <summary>
    /// Un aviso de la cuenta para leerlo, o nulo si no existe, es de otro club o no le llegó (sin
    /// distinguir los casos).
    /// </summary>
    Task<AvisoDeCuenta?> ObtenerParaLecturaAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, Guid avisoId, CancellationToken cancelacion = default);

    /// <summary>
    /// Pone la hora en todas las filas de la cuenta para ese aviso donde todavía es nula: la primera
    /// lectura no se mueve.
    /// </summary>
    Task MarcarLeidoAsync(Guid usuarioId, Guid avisoId, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>Marca como leídos todos los avisos visibles de la cuenta que siguen sin leer.</summary>
    Task MarcarTodosLeidosAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// De esas actividades, las que la cuenta puede abrir hoy (research §11): un entrenamiento si
    /// alguna de sus categorías es la actual de un jugador activo de la cuenta o ese jugador tiene su
    /// asistencia registrada; un partido si está jugado o un jugador activo de la cuenta está
    /// convocado. Respeta la limitación de la sesión.
    /// </summary>
    Task<IReadOnlySet<Guid>> DisponibilidadAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid>? limitacion,
        IReadOnlyCollection<Guid> entrenamientoIds,
        IReadOnlyCollection<Guid> partidoIds,
        CancellationToken cancelacion = default);
}

/// <summary>
/// Representa a un jugador activo que recibe un aviso al generarse.
/// Su responsabilidad es llevar el jugador y su cuenta.
/// No lleva datos personales.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="UsuarioId">Cuenta dueña del jugador.</param>
public record DestinatarioDeAviso(Guid UsuarioRolId, Guid UsuarioId);

/// <summary>
/// Representa a un jugador activo al que alude un aviso.
/// Su responsabilidad es llevar el jugador, su cuenta y sus nombres, para nombrarlo en el texto.
/// No lleva apellidos ni otros datos.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="UsuarioId">Cuenta dueña del jugador.</param>
/// <param name="Nombres">Nombres del jugador.</param>
public record JugadorDeAviso(Guid UsuarioRolId, Guid UsuarioId, string Nombres);

/// <summary>
/// Representa un aviso tal como lo ve una cuenta de jugador.
/// Su responsabilidad es llevar el aviso, si la cuenta lo leyó y cuándo.
/// No lleva destinatarios.
/// </summary>
/// <param name="Aviso">El aviso, sin destinatarios.</param>
/// <param name="Leido">Si ninguna fila de la cuenta para ese aviso está sin leer.</param>
/// <param name="LeidoEn">Cuándo lo leyó; nulo si no lo leyó.</param>
public record AvisoDeCuenta(AvisoDeActividad Aviso, bool Leido, DateTime? LeidoEn);
