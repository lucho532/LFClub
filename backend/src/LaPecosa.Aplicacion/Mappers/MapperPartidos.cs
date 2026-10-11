using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de los partidos y sus planillas en los DTOs del contrato (spec 009).
/// Su responsabilidad es armar el resumen, el detalle y la planilla, con la hora como <c>HH:mm</c>,
/// las categorías por año, los convocados por categoría copiada y apellidos, y el enlace a la ficha
/// de cada jugador solo si quien mira puede abrirla.
/// No decide quién ve qué ni consulta nada: lo que no se le pasa no sale.
/// </summary>
public static class MapperPartidos
{
    /// <summary>El partido en una lista, con la respuesta del convocado de la petición si se indica.</summary>
    public static PartidoResumenDto AResumen(Partido partido, bool faltaResultado, RespuestaFamilia? miRespuesta) =>
        new(
            partido.Id,
            partido.Rival,
            partido.Fecha,
            MapperActividades.Hora(partido.Hora),
            partido.Lugar,
            partido.Condicion,
            partido.Tipo,
            partido.NombreTorneo,
            partido.Estado,
            faltaResultado,
            partido.GolesClub,
            partido.GolesRival,
            Categorias(partido),
            miRespuesta);

    /// <summary>El detalle de un partido que quien pregunta puede ver.</summary>
    public static PartidoDto ADetalle(
        PartidoVisible visible, IReadOnlyList<GoleadorDePartidoDto>? goleadores, UltimoCambioDto? ultimoCambio)
    {
        var partido = visible.Partido;
        return new PartidoDto(
            partido.Id,
            partido.Rival,
            partido.Fecha,
            MapperActividades.Hora(partido.Hora),
            partido.Lugar,
            partido.Condicion,
            partido.Tipo,
            partido.NombreTorneo,
            partido.Estado,
            visible.FaltaResultado,
            partido.GolesClub,
            partido.GolesRival,
            Categorias(partido),
            visible.MiFila?.Respuesta,
            visible.Permisos,
            goleadores,
            ultimoCambio);
    }

    /// <summary>La planilla con los conteos de respuestas y, si se pasan, los convocables.</summary>
    public static PlanillaPartidoDto APlanilla(
        Guid partidoId, IReadOnlyList<PartidoJugador> convocados, IReadOnlyList<UsuarioRol> convocables, QuienPregunta quien) =>
        new(
            partidoId,
            convocados.Count(fila => fila.Respuesta == RespuestaFamilia.ASISTIRA),
            convocados.Count(fila => fila.Respuesta == RespuestaFamilia.NO_ASISTIRA),
            convocados.Count(fila => fila.Respuesta is null),
            convocados
                .OrderBy(fila => fila.Categoria?.Anio)
                .ThenBy(fila => fila.UsuarioRol!.Apellidos, StringComparer.CurrentCulture)
                .ThenBy(fila => fila.UsuarioRol!.Nombres, StringComparer.CurrentCulture)
                .Select(fila => new ConvocadoDto(
                    fila.UsuarioRolId,
                    fila.UsuarioRol!.Nombres,
                    fila.UsuarioRol.Apellidos,
                    ResolutorDeEnlaces.DeJugador(fila.UsuarioRol, quien),
                    MapperActividades.Categoria(fila.CategoriaId, fila.Categoria),
                    fila.Respuesta,
                    fila.Jugo,
                    fila.Goles))
                .ToList(),
            convocables
                .OrderBy(jugador => jugador.Categoria?.Anio)
                .ThenBy(jugador => jugador.Apellidos, StringComparer.CurrentCulture)
                .ThenBy(jugador => jugador.Nombres, StringComparer.CurrentCulture)
                .Select(jugador => new ConvocableDto(
                    jugador.Id,
                    jugador.Nombres,
                    jugador.Apellidos,
                    MapperActividades.Categoria(jugador.CategoriaId!.Value, jugador.Categoria)))
                .ToList());

    private static IReadOnlyList<CategoriaDeActividadDto> Categorias(Partido partido) =>
        MapperActividades.Categorias(partido.Categorias.Select(fila => (fila.CategoriaId, fila.Categoria)));
}
