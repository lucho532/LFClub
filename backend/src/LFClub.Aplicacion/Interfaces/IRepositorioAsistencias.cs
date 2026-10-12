using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a la asistencia y a las respuestas de un entrenamiento del club de la
/// petición (spec 009).
/// Su responsabilidad es leer los jugadores activos de unas categorías, las asistencias ya
/// registradas (con su categoría copiada) y las respuestas, y escribir la asistencia copiando la
/// categoría la primera vez y sin cambiarla al corregir (§13).
/// No decide quién marca a quién ni cuándo, y no recibe un identificador de club.
/// </summary>
public interface IRepositorioAsistencias
{
    /// <summary>Los jugadores activos y aprobados cuya categoría actual es una de esas, con su categoría.</summary>
    Task<IReadOnlyList<UsuarioRol>> JugadoresActivosAsync(IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default);

    /// <summary>Las asistencias registradas en el entrenamiento, con su jugador y la categoría copiada.</summary>
    Task<IReadOnlyList<AsistenciaEntrenamiento>> AsistenciasAsync(Guid entrenamientoId, CancellationToken cancelacion = default);

    /// <summary>Las respuestas de las familias al entrenamiento, por jugador.</summary>
    Task<IReadOnlyDictionary<Guid, RespuestaFamilia>> RespuestasAsync(Guid entrenamientoId, CancellationToken cancelacion = default);

    /// <summary>
    /// Registra o corrige la asistencia de cada jugador enviado: la fila nueva copia la categoría
    /// indicada; la que ya existe solo cambia si estuvo presente y cuándo se registró. Devuelve las
    /// categorías en las que alguna asistencia fue nueva o cambió de valor (spec 012, research §6);
    /// volver a guardar lo mismo no devuelve ninguna.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> RegistrarAsync(
        Guid entrenamientoId,
        IReadOnlyCollection<(UsuarioRol Jugador, Guid Categoria, bool Presente)> filas,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default);
}
