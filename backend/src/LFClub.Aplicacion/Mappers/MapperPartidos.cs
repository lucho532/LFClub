using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de los partidos y sus planillas en los DTOs del contrato (spec 009).
/// Su responsabilidad es armar el resumen, el detalle y la planilla, con la hora como <c>HH:mm</c>,
/// las categorías por año, los convocados por categoría copiada y apellidos con quién indicó lo de
/// cada uno, el enlace a la ficha de cada jugador solo si quien mira puede abrirla, y, en el
/// detalle, lo de su propio jugador para la familia y la publicación para quien ve la planilla
/// (spec 010).
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

    /// <summary>
    /// El detalle de un partido que quien pregunta puede ver. Lo de su propio jugador va solo a la
    /// cuenta de un convocado a un partido que ya empezó; la publicación y el origen, solo a quien ve
    /// la planilla de un partido jugado (spec 010).
    /// </summary>
    public static PartidoDto ADetalle(
        PartidoVisible visible, IReadOnlyList<GoleadorDePartidoDto>? goleadores, UltimoCambioDto? ultimoCambio)
    {
        var partido = visible.Partido;
        var miFila = visible.MiFila;
        var miParticipacion = PartidoVisible.Participa(partido, miFila, visible.Empezo)
            ? new MiParticipacionDto(miFila!.Jugo, miFila.Goles, miFila.IndicadoPor, miFila.IndicadoPor != OrigenResultado.GESTION)
            : null;
        var conPublicacion = visible.Permisos.VePlanilla && partido.Estado == EstadoPartido.JUGADO && partido.PublicadoEn is not null;
        var publicacion = conPublicacion
            ? new PublicacionResultadoDto(partido.PublicadoEn!.Value, partido.PublicadoPorNombre ?? string.Empty, partido.PublicadoPorFamilia)
            : null;
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
            ultimoCambio,
            miParticipacion,
            publicacion,
            conPublicacion ? partido.OrigenResultado : null);
    }

    /// <summary>
    /// La planilla con los conteos de respuestas, los equipos de las categorías del partido y, si se
    /// pasan, los convocables. Cada jugador lleva los equipos en los que juega hoy; el convocado cuya
    /// categoría copiada no es una del partido va marcado como añadido, solo para quien gestiona
    /// (spec 013, RF-009 y RF-021).
    /// </summary>
    public static PlanillaPartidoDto APlanilla(
        Guid partidoId,
        IReadOnlyList<PartidoJugador> convocados,
        IReadOnlyList<UsuarioRol> convocables,
        IReadOnlyList<Equipo> equipos,
        IReadOnlySet<Guid> categoriasDelPartido,
        bool gestiona,
        QuienPregunta quien) =>
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
                    fila.Goles,
                    fila.IndicadoPor is { } origen
                        ? new IndicacionDto(origen, fila.IndicadoPorNombre ?? string.Empty, fila.IndicadoEn!.Value)
                        : null,
                    fila.UsuarioRol.Equipos.Select(pertenencia => pertenencia.EquipoId).ToList(),
                    gestiona && !categoriasDelPartido.Contains(fila.CategoriaId)))
                .ToList(),
            convocables
                .OrderBy(jugador => jugador.Categoria?.Anio)
                .ThenBy(jugador => jugador.Apellidos, StringComparer.CurrentCulture)
                .ThenBy(jugador => jugador.Nombres, StringComparer.CurrentCulture)
                .Select(jugador => new ConvocableDto(
                    jugador.Id,
                    jugador.Nombres,
                    jugador.Apellidos,
                    MapperActividades.Categoria(jugador.CategoriaId!.Value, jugador.Categoria),
                    jugador.Equipos.Select(pertenencia => pertenencia.EquipoId).ToList()))
                .ToList(),
            equipos
                .Select(equipo => new EquipoDeActividadDto(
                    equipo.Id, equipo.Nombre, equipo.CategoriaId, equipo.Categoria?.Anio ?? 0, equipo.Activo))
                .ToList());

    private static IReadOnlyList<CategoriaDeActividadDto> Categorias(Partido partido) =>
        MapperActividades.Categorias(partido.Categorias.Select(fila => (fila.CategoriaId, fila.Categoria)));
}
