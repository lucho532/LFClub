using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de las cifras y los goleadores en los DTOs del contrato (spec 009).
/// Su responsabilidad es calcular el total como suma de las categorías, el porcentaje con
/// <see cref="CalculoDeAsistencia"/>, ordenar las categorías por año descendente y los goleadores de
/// mayor a menor y por apellidos y nombres, y poner a cada goleador el enlace a su ficha solo si
/// quien mira puede abrirla.
/// No consulta nada ni decide quién ve las cifras.
/// </summary>
public static class MapperEstadisticas
{
    /// <summary>Las estadísticas del jugador.</summary>
    public static EstadisticasJugadorDto AEstadisticas(Guid usuarioRolId, IReadOnlyList<CifrasDeCategoria> porCategoria)
    {
        var jugados = porCategoria.Sum(fila => fila.PartidosJugados);
        var goles = porCategoria.Sum(fila => fila.Goles);
        var registrados = porCategoria.Sum(fila => fila.Registrados);
        var asistidos = porCategoria.Sum(fila => fila.Asistidos);

        return new EstadisticasJugadorDto(
            usuarioRolId,
            new CifrasDto(jugados, goles, registrados, asistidos, CalculoDeAsistencia.Porcentaje(asistidos, registrados)),
            porCategoria
                .OrderByDescending(fila => fila.Categoria.Anio)
                .Select(fila => new CifrasDeCategoriaDto(
                    fila.PartidosJugados,
                    fila.Goles,
                    fila.Registrados,
                    fila.Asistidos,
                    CalculoDeAsistencia.Porcentaje(fila.Asistidos, fila.Registrados),
                    MapperActividades.Categoria(fila.Categoria.Id, fila.Categoria)))
                .ToList());
    }

    /// <summary>Los goleadores de una categoría, o solo las categorías si no se eligió ninguna.</summary>
    public static GoleadoresDto AGoleadores(
        IReadOnlyList<Categoria> conGoles,
        Categoria? elegida,
        IReadOnlyList<(UsuarioRol Jugador, int Goles)> goleadores,
        QuienPregunta quien) =>
        new(
            conGoles.Select(categoria => MapperActividades.Categoria(categoria.Id, categoria)).ToList(),
            elegida is null ? null : MapperActividades.Categoria(elegida.Id, elegida),
            goleadores
                .OrderByDescending(fila => fila.Goles)
                .ThenBy(fila => fila.Jugador.Apellidos, StringComparer.CurrentCulture)
                .ThenBy(fila => fila.Jugador.Nombres, StringComparer.CurrentCulture)
                .Select(fila => new GoleadorDePartidoDto(
                    fila.Jugador.Nombres, fila.Jugador.Apellidos, fila.Goles, ResolutorDeEnlaces.DeJugador(fila.Jugador, quien)))
                .ToList());
}
