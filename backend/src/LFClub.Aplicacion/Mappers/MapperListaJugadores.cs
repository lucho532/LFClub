using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de los jugadores del club en los grupos de la lista "Jugadores"
/// (spec 007, RF-002 y RF-005).
/// Su responsabilidad es agruparlos por categoría, ordenar los grupos por año de menor a mayor con
/// "Sin categoría" al final, ordenar los jugadores por apellidos y nombres y quedarse con los
/// equipos activos de cada uno, por nombre. Nunca devuelve un grupo vacío.
/// No consulta la base de datos ni decide quién aparece: recibe ya filtrados los jugadores que
/// quien pregunta puede ver, con su categoría y sus equipos cargados.
/// </summary>
public static class MapperListaJugadores
{
    /// <summary>
    /// Los grupos de esos jugadores. <paramref name="fotos"/> trae la fecha de la foto de quien
    /// tiene una; quien no aparece en ella no tiene foto. <paramref name="estados"/> trae el estado
    /// de cuenta de cada jugador, o es nulo si quien pregunta no ve las finanzas (spec 015).
    /// </summary>
    public static List<GrupoDeJugadoresDto> AGrupos(
        IEnumerable<UsuarioRol> jugadores,
        IReadOnlyDictionary<Guid, DateTime> fotos,
        IReadOnlyDictionary<Guid, EstadoDeCuenta>? estados = null) =>
        jugadores
            .GroupBy(jugador => jugador.Categoria?.Id)
            .OrderBy(grupo => grupo.Key is null)
            .ThenBy(grupo => grupo.First().Categoria?.Anio)
            .Select(grupo => new GrupoDeJugadoresDto(
                grupo.First().Categoria is { } categoria ? new CategoriaDeGrupoDto(categoria.Id, categoria.Anio) : null,
                grupo.PorApellidos().Select(jugador => AJugador(jugador, fotos, estados)).ToList()))
            .ToList();

    private static JugadorDeListaDto AJugador(
        UsuarioRol jugador,
        IReadOnlyDictionary<Guid, DateTime> fotos,
        IReadOnlyDictionary<Guid, EstadoDeCuenta>? estados) => new(
        jugador.Id,
        jugador.Nombres,
        jugador.Apellidos,
        jugador.Equipos
            .Select(fila => fila.Equipo
                ?? throw new InvalidOperationException("Los equipos del jugador deben venir cargados."))
            .Where(equipo => equipo.Activo)
            .Select(equipo => equipo.Nombre)
            .Order(OrdenDePersonas.Comparador)
            .ToList(),
        fotos.TryGetValue(jugador.Id, out var guardadaEn) ? guardadaEn : null,
        // Un jugador sin cargos no debe nada: está al día. Sin mapa, quien pregunta no ve el estado.
        estados is null ? null : estados.TryGetValue(jugador.Id, out var estado) ? estado : EstadoDeCuenta.AL_DIA);
}
