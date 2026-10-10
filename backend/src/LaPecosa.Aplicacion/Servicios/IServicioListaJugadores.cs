using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta de la lista "Jugadores" del club (spec 007, RF-001 a RF-005).
/// Su responsabilidad es devolver a cada quien los jugadores activos que puede ver, agrupados por
/// categoría: el PRESIDENTE y los DIRECTIVOS, todos, con "Sin categoría" al final; el ENTRENADOR,
/// solo los de las categorías activas en las que tiene una asignación activa.
/// No modifica nada, no devuelve datos de la ficha ni de pago y no da a un PRESIDENTE o a un
/// DIRECTIVO un alcance distinto por estar asignado como entrenador.
/// </summary>
public interface IServicioListaJugadores
{
    /// <summary>Los grupos de jugadores que ve ese integrante, ya ordenados.</summary>
    Task<IReadOnlyList<GrupoDeJugadoresDto>> ListarAsync(UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
