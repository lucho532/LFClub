namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa las cifras deportivas de un jugador, calculadas de lo registrado (spec 009, RF-026 y
/// RF-030).
/// Su responsabilidad es llevar los partidos jugados, los goles y la asistencia a entrenamientos.
/// No se guarda en ninguna tabla: se calcula en cada consulta.
/// </summary>
/// <param name="PartidosJugados">Partidos jugados en los que jugó.</param>
/// <param name="Goles">Goles en partidos jugados.</param>
/// <param name="EntrenamientosRegistrados">Entrenamientos no cancelados con su asistencia registrada.</param>
/// <param name="EntrenamientosAsistidos">De esos, a los que asistió.</param>
/// <param name="PorcentajeAsistencia">De 0 a 100; 0 si no hay registros.</param>
public record CifrasDto(
    int PartidosJugados, int Goles, int EntrenamientosRegistrados, int EntrenamientosAsistidos, int PorcentajeAsistencia);

/// <summary>
/// Representa las cifras de un jugador en una categoría: la copiada en cada registro (§13).
/// Su responsabilidad es llevar las cifras junto a la categoría.
/// No dice si la categoría sigue activa.
/// </summary>
/// <param name="PartidosJugados">Partidos jugados en los que jugó con esa categoría.</param>
/// <param name="Goles">Goles con esa categoría.</param>
/// <param name="EntrenamientosRegistrados">Asistencias registradas con esa categoría.</param>
/// <param name="EntrenamientosAsistidos">De esas, a las que asistió.</param>
/// <param name="PorcentajeAsistencia">De 0 a 100.</param>
/// <param name="Categoria">La categoría.</param>
public record CifrasDeCategoriaDto(
    int PartidosJugados,
    int Goles,
    int EntrenamientosRegistrados,
    int EntrenamientosAsistidos,
    int PorcentajeAsistencia,
    CategoriaDeActividadDto Categoria);
