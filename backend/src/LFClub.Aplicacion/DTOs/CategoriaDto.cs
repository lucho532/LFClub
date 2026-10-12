namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una categoría en la lista del club.
/// Su responsabilidad es llevar su año, si está activa, si todavía se puede borrar, cuántos
/// jugadores tiene, sus equipos activos y sus entrenadores asignados.
/// No lleva la lista de sus jugadores: eso es <see cref="CategoriaDetalleDto"/>.
/// </summary>
/// <param name="CategoriaId">Identificador de la categoría.</param>
/// <param name="Anio">Año de nacimiento que agrupa; es su nombre.</param>
/// <param name="Activa">Si está activa.</param>
/// <param name="SePuedeBorrar">Verdadero si nunca ha tenido jugadores ni entrenadores.</param>
/// <param name="NumeroJugadores">Jugadores de la categoría; cada uno cuenta una vez.</param>
/// <param name="Equipos">Equipos activos, por nombre.</param>
/// <param name="Entrenadores">Asignaciones activas, por apellidos.</param>
public record CategoriaDto(
    Guid CategoriaId,
    int Anio,
    bool Activa,
    bool SePuedeBorrar,
    int NumeroJugadores,
    IReadOnlyList<EquipoDto> Equipos,
    IReadOnlyList<EntrenadorDeCategoriaDto> Entrenadores);
