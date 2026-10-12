namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una categoría con la lista de sus jugadores.
/// Su responsabilidad es añadir a <see cref="CategoriaDto"/> todos los jugadores de la categoría,
/// de cualquier equipo o de ninguno, por apellidos.
/// No lleva de cada jugador más que lo que permite <see cref="JugadorDeCategoriaDto"/>.
/// </summary>
/// <param name="CategoriaId">Identificador de la categoría.</param>
/// <param name="Anio">Año de nacimiento que agrupa; es su nombre.</param>
/// <param name="Activa">Si está activa.</param>
/// <param name="SePuedeBorrar">Verdadero si nunca ha tenido jugadores ni entrenadores.</param>
/// <param name="NumeroJugadores">Jugadores de la categoría; cada uno cuenta una vez.</param>
/// <param name="Equipos">Equipos activos, por nombre.</param>
/// <param name="Entrenadores">Asignaciones activas, por apellidos.</param>
/// <param name="Jugadores">Jugadores de la categoría, por apellidos.</param>
public record CategoriaDetalleDto(
    Guid CategoriaId,
    int Anio,
    bool Activa,
    bool SePuedeBorrar,
    int NumeroJugadores,
    IReadOnlyList<EquipoDto> Equipos,
    IReadOnlyList<EntrenadorDeCategoriaDto> Entrenadores,
    IReadOnlyList<JugadorDeCategoriaDto> Jugadores)
    : CategoriaDto(CategoriaId, Anio, Activa, SePuedeBorrar, NumeroJugadores, Equipos, Entrenadores);
