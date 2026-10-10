using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las fotos propias de los jugadores del club de la petición (§14.2).
/// Su responsabilidad es leer la foto de un jugador, decir cuándo se cargó la de uno o la de
/// varios sin leer los bytes, guardarla sustituyendo a la anterior y borrarla.
/// No recibe un identificador de club para leer ni ve fotos de otro club. No valida la imagen, no
/// decide quién puede verla o cambiarla y no bloquea el club.
/// </summary>
public interface IRepositorioFotosJugador
{
    /// <summary>La foto de ese jugador, con su contenido y sin seguimiento; nulo si no tiene.</summary>
    Task<FotoJugador?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Fecha UTC de la última carga de la foto de ese jugador, sin leer los bytes; nula si no tiene.</summary>
    Task<DateTime?> FechaAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Fecha UTC de la última carga de la foto de cada uno de esos jugadores, en una sola consulta y
    /// sin leer los bytes. Quien no tiene foto no aparece en el resultado.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, DateTime>> FechasAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda la foto de ese jugador: inserta la fila o sustituye el contenido, el tipo y la fecha de
    /// la que hay; se guarda con la unidad de trabajo. Debe llamarse con el club ya bloqueado.
    /// </summary>
    Task GuardarAsync(
        Guid usuarioRolId,
        Guid clubId,
        byte[] contenido,
        string tipoContenido,
        DateTime guardadaEn,
        CancellationToken cancelacion = default);

    /// <summary>Borra de inmediato la foto de ese jugador; si no tiene, no hace nada.</summary>
    Task BorrarAsync(Guid usuarioRolId, CancellationToken cancelacion = default);
}
