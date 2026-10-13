namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un equipo al que va dirigido un entrenamiento, o uno de los equipos de las
/// categorías de un partido, tal como lo recibe quien ve la actividad (spec 013, RF-005).
/// Su responsabilidad es llevar el nombre del equipo, su categoría con el año y si sigue activo.
/// No lleva a sus jugadores ni a sus entrenadores.
/// </summary>
/// <param name="EquipoId">Identificador del equipo.</param>
/// <param name="Nombre">Nombre del equipo.</param>
/// <param name="CategoriaId">Categoría del equipo.</param>
/// <param name="Anio">Año de la categoría.</param>
/// <param name="Activo">Falso si se desactivó después de dirigirle el entrenamiento (RF-026).</param>
public record EquipoDeActividadDto(Guid EquipoId, string Nombre, Guid CategoriaId, int Anio, bool Activo);
