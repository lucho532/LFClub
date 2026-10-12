namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa las cuatro cifras del inicio del PRESIDENTE y de los DIRECTIVOS (spec 007, RF-011).
/// Su responsabilidad es llevar lo que el club tiene hoy, calculado en cada consulta.
/// No se guarda en ninguna parte ni lleva datos de pagos ni de entrenamientos.
/// </summary>
/// <param name="Jugadores">Jugadores aprobados y activos, con o sin categoría.</param>
/// <param name="Equipos">Equipos activos de categorías activas.</param>
/// <param name="Entrenadores">
/// Personas distintas asignadas como entrenador a una categoría activa, de cualquier rol, más los
/// integrantes con rol ENTRENADOR que no tienen ninguna asignación.
/// </param>
/// <param name="Categorias">Categorías activas.</param>
public record ResumenClubDto(int Jugadores, int Equipos, int Entrenadores, int Categorias);
