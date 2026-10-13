using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los entrenamientos del club de la petición (spec 009).
/// Su responsabilidad es guardarlos y cambiarlos, leerlos con sus categorías y, para las listas,
/// traer solo los que quien pregunta puede ver: la condición de
/// <c>ReglaVisibilidadDeEntrenamiento</c> va traducida al <c>WHERE</c>, así que nunca trae ni
/// cuenta uno que no se ve. Lee también la respuesta y la asistencia del jugador de la petición.
/// No recibe un identificador de club ni ve entrenamientos de otro club, no decide quién ve qué y
/// nunca borra un entrenamiento.
/// </summary>
public interface IRepositorioEntrenamientos
{
    /// <summary>Guarda de inmediato un entrenamiento nuevo con sus categorías.</summary>
    Task AgregarAsync(Entrenamiento entrenamiento, CancellationToken cancelacion = default);

    /// <summary>Un entrenamiento del club con sus categorías (y el año de cada una), o nulo si no existe en él.</summary>
    Task<Entrenamiento?> ObtenerAsync(Guid entrenamientoId, CancellationToken cancelacion = default);

    /// <summary>Cambia fecha, horas, lugar y nota.</summary>
    Task CambiarDatosAsync(
        Guid entrenamientoId,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        string lugar,
        string? nota,
        CancellationToken cancelacion = default);

    /// <summary>Deja el entrenamiento en ese estado; no toca sus categorías ni sus respuestas.</summary>
    Task CambiarEstadoAsync(Guid entrenamientoId, EstadoEntrenamiento estado, CancellationToken cancelacion = default);

    /// <summary>Los entrenamientos visibles para quien pregunta entre dos fechas, ambas incluidas, por fecha y hora.</summary>
    Task<IReadOnlyList<Entrenamiento>> ListarVisiblesAsync(
        DateOnly desde, DateOnly hasta, QuienPregunta quien, CancellationToken cancelacion = default);

    /// <summary>
    /// Hasta <paramref name="maximo"/> entrenamientos visibles que no han terminado, cancelados
    /// incluidos, por fecha y hora de inicio.
    /// </summary>
    Task<IReadOnlyList<Entrenamiento>> ProximosVisiblesAsync(
        DateOnly hoy, TimeOnly ahora, QuienPregunta quien, int maximo, CancellationToken cancelacion = default);

    /// <summary>La respuesta de la familia de ese jugador, o nula si no respondió.</summary>
    Task<RespuestaFamilia?> RespuestaDeAsync(Guid entrenamientoId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Las respuestas de la familia de ese jugador a esos entrenamientos, por entrenamiento.</summary>
    Task<IReadOnlyDictionary<Guid, RespuestaFamilia>> RespuestasDeAsync(
        IReadOnlyCollection<Guid> entrenamientoIds, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Si ese jugador está en la lista guardada del entrenamiento: añadido por quien dirige o
    /// congelado al desactivar un equipo (spec 013).
    /// </summary>
    Task<bool> EnListaDeAsync(Guid entrenamientoId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Si ese jugador estuvo presente, o nulo si nadie registró su asistencia.</summary>
    Task<bool?> AsistenciaDeAsync(Guid entrenamientoId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Guarda o reemplaza la respuesta de la familia de ese jugador.</summary>
    Task ResponderAsync(
        Guid entrenamientoId, UsuarioRol jugador, RespuestaFamilia respuesta, DateTime ahoraUtc, CancellationToken cancelacion = default);
}
