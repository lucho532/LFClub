using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los convocados de los partidos del club de la petición (spec 009): la
/// planilla, los jugadores que se pueden convocar y los cambios de la convocatoria.
/// Su responsabilidad es leer y escribir las filas de <see cref="PartidoJugador"/> (también lo que
/// publica o indica la familia de un convocado, spec 010), copiando al
/// convocar la categoría actual del jugador y conservando la fila (con su categoría copiada y su
/// respuesta) de quien sigue convocado.
/// No decide quién puede convocar ni a quién (eso lo hace el servicio), y no recibe un
/// identificador de club.
/// </summary>
public interface IRepositorioConvocatoria
{
    /// <summary>
    /// Los equipos activos de esas categorías, con su categoría, por año y nombre (spec 013, RF-009).
    /// </summary>
    Task<IReadOnlyList<Equipo>> EquiposActivosAsync(
        IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Los convocados de un partido, con su integrante (y los equipos en los que juega hoy) y la
    /// categoría copiada.
    /// </summary>
    Task<IReadOnlyList<PartidoJugador>> ConvocadosAsync(Guid partidoId, CancellationToken cancelacion = default);

    /// <summary>
    /// Los jugadores activos y aprobados del club cuya categoría actual es una de esas y que no están
    /// convocados a ese partido, con su categoría.
    /// </summary>
    Task<IReadOnlyList<UsuarioRol>> ConvocablesAsync(
        Guid partidoId, IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default);

    /// <summary>Los integrantes del club con esos identificadores, de cualquier rol y estado.</summary>
    Task<IReadOnlyList<UsuarioRol>> IntegrantesAsync(IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Convoca a <paramref name="altas"/> con su categoría actual copiada y borra, con su respuesta,
    /// las filas de <paramref name="bajas"/>. Las demás no se tocan.
    /// </summary>
    Task ReemplazarAsync(
        Guid partidoId,
        IReadOnlyCollection<UsuarioRol> altas,
        IReadOnlyCollection<Guid> bajas,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Deja el partido jugado con ese marcador, quién lo cambió (con su nombre copiado) y origen
    /// GESTION; si no tenía marcador, también la primera publicación. Cada convocado de
    /// <paramref name="filas"/> queda como se indica, y los corregidos pasan a GESTION con quien los
    /// corrigió (spec 010).
    /// </summary>
    Task GuardarResultadoAsync(
        Guid partidoId,
        int golesClub,
        int golesRival,
        IReadOnlyCollection<FilaGuardada> filas,
        DateTime ahoraUtc,
        Guid autorUsuarioId,
        string autorNombre,
        CancellationToken cancelacion = default);

    /// <summary>La suma de goles de los convocados del partido que no son ese jugador.</summary>
    Task<int> GolesDeLosDemasAsync(Guid partidoId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Publica el marcador a nombre de la familia de un convocado (el nombre del jugador, copiado) y
    /// deja el partido jugado con origen FAMILIA. No hace nada si el partido ya tiene marcador.
    /// </summary>
    Task PublicarPorFamiliaAsync(
        Guid partidoId,
        int golesClub,
        int golesRival,
        DateTime ahoraUtc,
        Guid autorUsuarioId,
        string autorNombre,
        CancellationToken cancelacion = default);

    /// <summary>Guarda lo que indica la familia de un convocado: si jugó, sus goles y el sello FAMILIA.</summary>
    Task IndicarPorFamiliaAsync(
        Guid partidoId,
        Guid usuarioRolId,
        bool jugo,
        int goles,
        DateTime ahoraUtc,
        Guid autorUsuarioId,
        string autorNombre,
        CancellationToken cancelacion = default);

    /// <summary>Guarda o reemplaza la respuesta de la familia en la fila del convocado.</summary>
    Task ResponderAsync(
        Guid partidoId, Guid usuarioRolId, RespuestaFamilia respuesta, DateTime ahoraUtc, CancellationToken cancelacion = default);
}

/// <summary>
/// Representa cómo queda un convocado al guardar quien gestiona el resultado (spec 010).
/// Su responsabilidad es llevar si jugó, sus goles y si esta vez se corrige lo que tenía, ya
/// decidido por <c>ReglaCorreccionDeIndicacion</c>.
/// No decide nada.
/// </summary>
/// <param name="UsuarioRolId">Jugador convocado.</param>
/// <param name="Jugo">Si jugó.</param>
/// <param name="Goles">Goles que marcó.</param>
/// <param name="Corregida">Si pasa a GESTION, sellada con quien guarda.</param>
public readonly record struct FilaGuardada(Guid UsuarioRolId, bool Jugo, int Goles, bool Corregida);
