using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los jugadores del club de la petición: los integrantes con rol JUGADOR,
/// ingreso aprobado y no retirados, más los que el club retiró.
/// Su responsabilidad es leer las listas de jugadores y al jugador de una ficha, y cambiar su
/// categoría, su retiro, su reincorporación, su documento de identidad y su identidad con
/// sentencias condicionadas, de modo que nunca se le asigne categoría a quien
/// no es jugador del club ni se mueva a quien ya tiene una cuando la ubicación es automática.
/// No recibe un identificador de club ni ve integrantes de otro club. No decide a qué categoría va
/// cada jugador ni bloquea el club: eso lo hacen los servicios.
/// </summary>
public interface IRepositorioJugadores
{
    /// <summary>Un integrante del club, con cualquier rol y estado, sin sus equipos; no lo sigue.</summary>
    Task<UsuarioRol?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Un integrante del club con los equipos en los que juega; no lo sigue.</summary>
    Task<UsuarioRol?> ObtenerConEquiposAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// El jugador de una ficha (RF-004): el integrante con rol JUGADOR e ingreso aprobado, activo o
    /// retirado, con su cuenta, su categoría y los equipos en los que juega; no lo sigue. Nulo para
    /// cualquier otro rol y para quien está en espera.
    /// </summary>
    Task<UsuarioRol?> ObtenerParaFichaAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Todos los jugadores del club (aprobados y no retirados), cada uno con su categoría y con los
    /// equipos en los que juega, en una sola consulta (spec 007).
    /// </summary>
    Task<IReadOnlyList<UsuarioRol>> ListarDelClubAsync(CancellationToken cancelacion = default);

    /// <summary>Jugadores del club sin categoría, cada uno sin equipos.</summary>
    Task<IReadOnlyList<UsuarioRol>> ListarSinCategoriaAsync(CancellationToken cancelacion = default);

    /// <summary>Jugadores de una categoría, cada uno con los equipos en los que juega.</summary>
    Task<IReadOnlyList<UsuarioRol>> ListarDeCategoriaAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>Jugadores retirados del club, del retiro más reciente al más antiguo.</summary>
    Task<IReadOnlyList<UsuarioRol>> ListarRetiradosAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Ubicación automática de uno: pone a ese jugador en la categoría solo si es jugador del club
    /// y no tiene categoría. Devuelve si lo ubicó.
    /// </summary>
    Task<bool> UbicarSiNoTieneAsync(Guid usuarioRolId, Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>
    /// Ubicación automática de un año: pone en la categoría a todos los jugadores del club sin
    /// categoría nacidos ese año. Devuelve cuántos entraron.
    /// </summary>
    Task<int> RecogerDelAnioAsync(Guid categoriaId, int anio, CancellationToken cancelacion = default);

    /// <summary>
    /// Ubicación a mano: pone a ese jugador en la categoría, tenga o no otra, solo si es jugador
    /// del club. Devuelve si lo cambió.
    /// </summary>
    Task<bool> CambiarCategoriaAsync(Guid usuarioRolId, Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>Saca al integrante de todos los equipos en los que juega.</summary>
    Task SacarDeSusEquiposAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra lo pendiente del jugador en las actividades (spec 009, RF-033): sus convocatorias a
    /// partidos que no están jugados y las respuestas de su familia a entrenamientos que no han
    /// empezado en hora de Colombia. Su asistencia y lo que jugó no se tocan.
    /// </summary>
    Task BorrarPendientesDeActividadesAsync(
        Guid usuarioRolId, DateOnly hoy, TimeOnly ahora, CancellationToken cancelacion = default);

    /// <summary>
    /// Retira al jugador solo si es jugador del club: lo deja inactivo, sin categoría y con quién
    /// lo retiró y cuándo, los tres datos a la vez. Devuelve si lo retiró.
    /// </summary>
    Task<bool> RetirarAsync(
        Guid usuarioRolId,
        DateTime ahoraUtc,
        Guid retiradoPorUsuarioId,
        string retiradoPorNombre,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Reincorpora al jugador solo si está retirado: lo deja activo y vacía a la vez los tres datos
    /// del retiro. Devuelve si lo reincorporó.
    /// </summary>
    Task<bool> ReincorporarAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si otro integrante del club, de cualquier rol, activo o retirado, tiene ya ese número
    /// de documento normalizado (RF-023 de la 005).
    /// </summary>
    Task<bool> ExisteDocumentoEnOtroAsync(Guid usuarioRolId, string numeroDocumento, CancellationToken cancelacion = default);

    /// <summary>
    /// Cambia el tipo y el número de documento de ese jugador aprobado, activo o retirado, sobre su
    /// misma fila: no crea otro integrante ni toca nada más (RF-022 de la 005). Devuelve si lo cambió.
    /// </summary>
    Task<bool> CambiarDocumentoAsync(
        Guid usuarioRolId, TipoDocumento tipoDocumento, string numeroDocumento, CancellationToken cancelacion = default);

    /// <summary>
    /// Corrige los nombres, los apellidos y la fecha de nacimiento de ese jugador aprobado, activo o
    /// retirado, sobre su misma fila. No lo mueve de categoría. Devuelve si lo corrigió.
    /// </summary>
    Task<bool> CorregirIdentidadAsync(
        Guid usuarioRolId,
        string nombres,
        string apellidos,
        DateOnly fechaNacimiento,
        CancellationToken cancelacion = default);
}
