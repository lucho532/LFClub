using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los convocados de los partidos del club de la petición (spec 009): la
/// planilla, los jugadores que se pueden convocar y los cambios de la convocatoria.
/// Su responsabilidad es leer y escribir las filas de <see cref="PartidoJugador"/>, copiando al
/// convocar la categoría actual del jugador y conservando la fila (con su categoría copiada y su
/// respuesta) de quien sigue convocado.
/// No decide quién puede convocar ni a quién (eso lo hace el servicio), y no recibe un
/// identificador de club.
/// </summary>
public interface IRepositorioConvocatoria
{
    /// <summary>Los convocados de un partido, con su integrante y la categoría copiada.</summary>
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
    /// Deja el partido jugado con ese marcador y quién lo cambió (con su nombre copiado), y a cada
    /// convocado con lo enviado: los que no aparecen quedan como que no jugaron y sin goles.
    /// </summary>
    Task GuardarResultadoAsync(
        Guid partidoId,
        int golesClub,
        int golesRival,
        IReadOnlyCollection<FilaDeResultado> filas,
        DateTime ahoraUtc,
        Guid autorUsuarioId,
        string autorNombre,
        CancellationToken cancelacion = default);

    /// <summary>Guarda o reemplaza la respuesta de la familia en la fila del convocado.</summary>
    Task ResponderAsync(
        Guid partidoId, Guid usuarioRolId, RespuestaFamilia respuesta, DateTime ahoraUtc, CancellationToken cancelacion = default);
}
