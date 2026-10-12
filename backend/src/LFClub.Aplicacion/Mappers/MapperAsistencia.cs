using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Implementaciones;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de la planilla de un entrenamiento en su DTO (spec 009).
/// Su responsabilidad es ordenar los jugadores por categoría y apellidos, ponerles su respuesta y el
/// enlace a su ficha solo si quien mira puede abrirla, y contar las respuestas de los que salen.
/// No decide qué jugadores salen ni consulta nada.
/// </summary>
public static class MapperAsistencia
{
    /// <summary>La planilla con sus conteos.</summary>
    public static PlanillaEntrenamientoDto APlanilla(
        Guid entrenamientoId,
        IReadOnlyList<FilaDePlanilla> filas,
        IReadOnlyDictionary<Guid, RespuestaFamilia> respuestas,
        bool marcable,
        QuienPregunta quien)
    {
        RespuestaFamilia? RespuestaDe(Guid id) => respuestas.TryGetValue(id, out var respuesta) ? respuesta : null;

        return new PlanillaEntrenamientoDto(
            entrenamientoId,
            filas.Count(fila => RespuestaDe(fila.Jugador.Id) == RespuestaFamilia.ASISTIRA),
            filas.Count(fila => RespuestaDe(fila.Jugador.Id) == RespuestaFamilia.NO_ASISTIRA),
            filas.Count(fila => RespuestaDe(fila.Jugador.Id) is null),
            filas
                .OrderBy(fila => fila.Categoria?.Anio)
                .ThenBy(fila => fila.Jugador.Apellidos, StringComparer.CurrentCulture)
                .ThenBy(fila => fila.Jugador.Nombres, StringComparer.CurrentCulture)
                .Select(fila => new JugadorDePlanillaEntrenamientoDto(
                    fila.Jugador.Id,
                    fila.Jugador.Nombres,
                    fila.Jugador.Apellidos,
                    ResolutorDeEnlaces.DeJugador(fila.Jugador, quien),
                    MapperActividades.Categoria(fila.CategoriaId, fila.Categoria),
                    RespuestaDe(fila.Jugador.Id),
                    fila.Presente,
                    marcable))
                .ToList());
    }
}
