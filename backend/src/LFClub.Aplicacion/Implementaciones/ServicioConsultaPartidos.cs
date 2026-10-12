using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que consulta partidos.
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAActividad"/> para el detalle y
/// pedir al repositorio solo los visibles para las listas, con la respuesta propia solo para la
/// cuenta de un jugador convocado. En un partido jugado añade sus goleadores para todos y, para
/// quien ve la planilla, quién cambió el resultado por última vez (RF-019, RF-020); lo de su propio
/// jugador para la familia y la publicación del resultado los añade <see cref="MapperPartidos"/>
/// (spec 010). Las operaciones
/// que cambian un partido responden con lo que arma este servicio.
/// No modifica datos, no decide quién ve qué y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioConsultaPartidos : IServicioConsultaPartidos
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioPartidos _partidos;
    private readonly IRepositorioConvocatoria _convocatoria;
    private readonly ResolutorDeEnlaces _enlaces;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaPartidos(
        AccesoAActividad acceso,
        IRepositorioPartidos partidos,
        IRepositorioConvocatoria convocatoria,
        ResolutorDeEnlaces enlaces,
        IReloj reloj)
    {
        _acceso = acceso;
        _partidos = partidos;
        _convocatoria = convocatoria;
        _enlaces = enlaces;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PartidoResumenDto>> ListarAsync(
        EstadoPartido? estado, Guid? categoriaId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var quien = await _acceso.QuienPreguntaAsync(quienPregunta, cancelacion);
        var partidos = await _partidos.ListarVisiblesAsync(estado, categoriaId, quien, cancelacion);
        var ordenados = partidos
            .Where(partido => partido.Estado == EstadoPartido.PROXIMO)
            .Concat(partidos
                .Where(partido => partido.Estado != EstadoPartido.PROXIMO)
                .OrderByDescending(partido => partido.Fecha)
                .ThenByDescending(partido => partido.Hora))
            .ToList();
        return await ResumenesAsync(_partidos, _reloj, ordenados, quien, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PartidoDto> ObtenerAsync(Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var visible = await _acceso.PartidoAsync(partidoId, quienPregunta, cancelacion);
        var partido = visible.Partido;
        if (partido.Estado != EstadoPartido.JUGADO)
        {
            return MapperPartidos.ADetalle(visible, null, null);
        }

        var goleadores = (await _convocatoria.ConvocadosAsync(partidoId, cancelacion))
            .Where(fila => fila.Goles > 0)
            .OrderByDescending(fila => fila.Goles)
            .ThenBy(fila => fila.UsuarioRol!.Apellidos, StringComparer.CurrentCulture)
            .ThenBy(fila => fila.UsuarioRol!.Nombres, StringComparer.CurrentCulture)
            .Select(fila => new GoleadorDePartidoDto(
                fila.UsuarioRol!.Nombres,
                fila.UsuarioRol.Apellidos,
                fila.Goles,
                ResolutorDeEnlaces.DeJugador(fila.UsuarioRol, visible.Quien)))
            .ToList();

        UltimoCambioDto? ultimoCambio = null;
        if (visible.Permisos.VePlanilla && partido.ResultadoCambiadoEn is { } cambiadoEn)
        {
            var autores = await _enlaces.AutoresAsync([partido.ResultadoCambiadoPorUsuarioId], cancelacion);
            ultimoCambio = new UltimoCambioDto(
                cambiadoEn,
                partido.ResultadoCambiadoPorNombre ?? string.Empty,
                false,
                partido.ResultadoCambiadoPorUsuarioId is { } autorId ? autores.GetValueOrDefault(autorId) : null);
        }

        return MapperPartidos.ADetalle(visible, goleadores, ultimoCambio);
    }

    /// <summary>
    /// Los partidos de una lista como resúmenes, con la marca de si falta el resultado; a la cuenta
    /// de un jugador, con su propia respuesta, leída en una sola consulta.
    /// </summary>
    public static async Task<IReadOnlyList<PartidoResumenDto>> ResumenesAsync(
        IRepositorioPartidos repositorio,
        IReloj reloj,
        IReadOnlyList<Partido> partidos,
        QuienPregunta quien,
        CancellationToken cancelacion)
    {
        var respuestas = quien.EsJugador && partidos.Count > 0
            ? await repositorio.RespuestasDeAsync(partidos.Select(partido => partido.Id).ToList(), quien.UsuarioRolId, cancelacion)
            : new Dictionary<Guid, RespuestaFamilia>();
        var (hoy, ahora) = HoraDeColombia.Ahora(reloj);

        return partidos
            .Select(partido => MapperPartidos.AResumen(
                partido,
                partido.Estado == EstadoPartido.PROXIMO && ReglaMomentoDeActividad.YaEmpezo(partido.Fecha, partido.Hora, hoy, ahora),
                respuestas.TryGetValue(partido.Id, out var respuesta) ? respuesta : null))
            .ToList();
    }
}
