using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los partidos del club de la petición (spec 009).
/// Su responsabilidad es guardarlos y cambiarlos, leerlos con sus categorías, leer la fila del
/// convocado de la petición y, para las listas y el calendario, traer solo los que quien pregunta
/// puede ver: la condición de <c>ReglaVisibilidadDePartido</c> va traducida al <c>WHERE</c>, así que
/// nunca trae ni cuenta un partido próximo al que no tiene acceso (§17.2, CE-001).
/// No recibe un identificador de club ni ve partidos de otro club, no decide quién ve qué, no toca
/// la convocatoria (ver <see cref="IRepositorioConvocatoria"/>) y nunca borra un partido.
/// </summary>
public interface IRepositorioPartidos
{
    /// <summary>Guarda de inmediato un partido nuevo con sus categorías y sin convocados.</summary>
    Task AgregarAsync(Partido partido, CancellationToken cancelacion = default);

    /// <summary>Un partido del club con sus categorías (y el año de cada una), o nulo si no existe en él.</summary>
    Task<Partido?> ObtenerAsync(Guid partidoId, CancellationToken cancelacion = default);

    /// <summary>La fila de ese jugador en la convocatoria, o nula si no está convocado.</summary>
    Task<PartidoJugador?> FilaDeAsync(Guid partidoId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Cambia rival, fecha, hora, lugar, condición, tipo y torneo con los valores de <paramref name="datos"/>.</summary>
    Task CambiarDatosAsync(Guid partidoId, Partido datos, CancellationToken cancelacion = default);

    /// <summary>Cambia solo el rival del partido; nulo lo deja por definir.</summary>
    Task CambiarRivalAsync(Guid partidoId, string? rival, CancellationToken cancelacion = default);

    /// <summary>Deja el partido en ese estado; no toca sus categorías ni su convocatoria.</summary>
    Task CambiarEstadoAsync(Guid partidoId, EstadoPartido estado, CancellationToken cancelacion = default);

    /// <summary>Los partidos visibles para quien pregunta, opcionalmente de un estado o de una categoría.</summary>
    Task<IReadOnlyList<Partido>> ListarVisiblesAsync(
        EstadoPartido? estado, Guid? categoriaId, QuienPregunta quien, CancellationToken cancelacion = default);

    /// <summary>Los partidos visibles para quien pregunta entre dos fechas, ambas incluidas, por fecha y hora.</summary>
    Task<IReadOnlyList<Partido>> EnRangoVisiblesAsync(
        DateOnly desde, DateOnly hasta, QuienPregunta quien, CancellationToken cancelacion = default);

    /// <summary>Las respuestas de la familia de ese jugador a esos partidos, por partido.</summary>
    Task<IReadOnlyDictionary<Guid, RespuestaFamilia>> RespuestasDeAsync(
        IReadOnlyCollection<Guid> partidoIds, Guid usuarioRolId, CancellationToken cancelacion = default);
}
