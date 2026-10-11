using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el cálculo en SQL de las estadísticas deportivas del club de la petición (spec 009,
/// research §9).
/// Su responsabilidad es agrupar por la categoría copiada en cada registro: partidos jugados y goles
/// en partidos jugados, asistencia en entrenamientos no cancelados, y goles por jugador en una
/// categoría, retirados incluidos.
/// No guarda nada, no decide quién ve las cifras y no recibe un identificador de club.
/// </summary>
public interface IRepositorioEstadisticas
{
    /// <summary>Las cifras de un jugador por cada categoría en la que tiene registros.</summary>
    Task<IReadOnlyList<CifrasDeCategoria>> DelJugadorAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Las categorías del club con al menos un gol en partidos jugados.</summary>
    Task<IReadOnlyList<Categoria>> CategoriasConGolesAsync(CancellationToken cancelacion = default);

    /// <summary>Una categoría del club, activa o no, o nula si no existe en él.</summary>
    Task<Categoria?> CategoriaAsync(Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>Los jugadores con goles en esa categoría y cuántos, en partidos jugados.</summary>
    Task<IReadOnlyList<(UsuarioRol Jugador, int Goles)>> GoleadoresAsync(Guid categoriaId, CancellationToken cancelacion = default);
}

/// <summary>
/// Representa lo que se cuenta de un jugador en una categoría.
/// Su responsabilidad es llevar los cuatro números sin calcular el porcentaje.
/// No lleva el jugador.
/// </summary>
/// <param name="Categoria">La categoría copiada en los registros.</param>
/// <param name="PartidosJugados">Filas en partidos jugados en las que jugó.</param>
/// <param name="Goles">Goles en partidos jugados.</param>
/// <param name="Registrados">Asistencias en entrenamientos no cancelados.</param>
/// <param name="Asistidos">De esas, presentes.</param>
public record CifrasDeCategoria(Categoria Categoria, int PartidosJugados, int Goles, int Registrados, int Asistidos);
