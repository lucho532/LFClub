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
/// Representa el servicio de la planilla y la asistencia de un entrenamiento (spec 009, RF-022,
/// RF-024 y RF-025; data-model "Planilla de un entrenamiento").
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAActividad"/>, limitar la planilla
/// a las categorías marcables con <see cref="ReglaAlcanceDeAsistencia"/> (los jugadores activos de
/// esas categorías y los que ya tienen asistencia en ellas) y registrar la asistencia solo en un
/// entrenamiento programado cuya fecha ya llegó, y solo de jugadores de la planilla. La respuesta de
/// la familia no se toca: son datos distintos (RF-025). Al registrar toma el bloqueo del club,
/// vuelve a leer el entrenamiento y, en la misma transacción, avisa a las familias de las categorías
/// en las que una asistencia fue nueva o cambió (spec 012), sin decir quién asistió ni quién faltó.
/// No decide quién ve el entrenamiento y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioAsistencia : IServicioAsistencia
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioAsistencias _asistencias;
    private readonly IRepositorioAudiencia _audiencia;
    private readonly IRepositorioClub _club;
    private readonly IServicioAvisosDeActividad _avisos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAsistencia(
        AccesoAActividad acceso,
        IRepositorioAsistencias asistencias,
        IRepositorioAudiencia audiencia,
        IRepositorioClub club,
        IServicioAvisosDeActividad avisos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _acceso = acceso;
        _asistencias = asistencias;
        _audiencia = audiencia;
        _club = club;
        _avisos = avisos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PlanillaEntrenamientoDto> PlanillaAsync(
        Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var visible = await ConPlanillaAsync(entrenamientoId, quienPregunta, cancelacion);
        return await LeerAsync(visible, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PlanillaEntrenamientoDto> RegistrarAsync(
        Guid entrenamientoId, RegistrarAsistenciaDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var enviadas = Validar(datos);
        var visible = await ConPlanillaAsync(entrenamientoId, quienPregunta, cancelacion);
        var entrenamiento = visible.Entrenamiento;
        if (entrenamiento.Estado == EstadoEntrenamiento.CANCELADO)
        {
            throw ErroresDeActividades.ActividadCancelada();
        }

        if (!ReglaMomentoDeActividad.SuFechaLlego(entrenamiento.Fecha, HoraDeColombia.Ahora(_reloj).Hoy))
        {
            throw ErroresDeActividades.EntrenamientoFuturo();
        }

        var planilla = (await FilasAsync(visible, cancelacion)).ToDictionary(fila => fila.Jugador.Id);
        if (enviadas.Any(enviada => !planilla.ContainsKey(enviada.UsuarioRolId)))
        {
            throw ErroresDeActividades.JugadorFueraDeLaActividad();
        }

        var filas = enviadas
            .Select(enviada => (planilla[enviada.UsuarioRolId].Jugador, planilla[enviada.UsuarioRolId].CategoriaId, enviada.Presente!.Value))
            .ToList();
        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var vigente = (await _acceso.EntrenamientoAsync(entrenamientoId, quienPregunta, cancelacion)).Entrenamiento;
                if (vigente.Estado == EstadoEntrenamiento.CANCELADO)
                {
                    throw ErroresDeActividades.ActividadCancelada();
                }

                var conCambio = await _asistencias.RegistrarAsync(entrenamientoId, filas, _reloj.AhoraUtc, cancelacion);
                if (conCambio.Count > 0)
                {
                    await _avisos.AsistenciaRegistradaAsync(vigente, conCambio, quienPregunta.UsuarioId, cancelacion);
                }
            },
            cancelacion);

        return await LeerAsync(visible, cancelacion);
    }

    /// <summary>El entrenamiento, si quien pregunta lo ve (si no, 404) y puede ver su planilla (si no, 403).</summary>
    private async Task<EntrenamientoVisible> ConPlanillaAsync(
        Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion)
    {
        var visible = await _acceso.EntrenamientoAsync(entrenamientoId, quienPregunta, cancelacion);
        if (!visible.Permisos.VePlanilla)
        {
            throw ErroresDeActividades.NoGestionaActividad();
        }

        return visible;
    }

    private async Task<PlanillaEntrenamientoDto> LeerAsync(EntrenamientoVisible visible, CancellationToken cancelacion)
    {
        var entrenamiento = visible.Entrenamiento;
        var filas = await FilasAsync(visible, cancelacion);
        var respuestas = await _asistencias.RespuestasAsync(entrenamiento.Id, cancelacion);
        var marcable = entrenamiento.Estado == EstadoEntrenamiento.PROGRAMADO
            && ReglaMomentoDeActividad.SuFechaLlego(entrenamiento.Fecha, HoraDeColombia.Ahora(_reloj).Hoy);
        return MapperAsistencia.APlanilla(entrenamiento.Id, filas, respuestas, marcable, visible.Quien);
    }

    /// <summary>
    /// Los jugadores de la planilla (spec 013, RF-021): los de la audiencia (los de los equipos del
    /// entrenamiento y los congelados, o los de sus categorías si no tiene equipos) que están en una
    /// categoría marcable, más los que ya tienen asistencia en una categoría marcable con la
    /// categoría copiada y, solo para quien dirige, los añadidos, de cualquier categoría.
    /// </summary>
    private async Task<IReadOnlyList<FilaDePlanilla>> FilasAsync(EntrenamientoVisible visible, CancellationToken cancelacion)
    {
        var quien = visible.Quien;
        var entrenamientoId = visible.Entrenamiento.Id;
        var marcables = ReglaAlcanceDeAsistencia.CategoriasMarcables(
            quien.Rol, visible.Entrenamiento.Categorias.Select(fila => fila.CategoriaId).ToList(), quien.CategoriasQueEntrena);
        var anadidos = visible.Permisos.Dirige
            ? (await _audiencia.AnadidosAsync(entrenamientoId, cancelacion)).Select(jugador => jugador.UsuarioRolId).ToHashSet()
            : new HashSet<Guid>();

        var conAsistencia = (await _asistencias.AsistenciasAsync(entrenamientoId, cancelacion))
            .Where(fila => marcables.Contains(fila.CategoriaId) || anadidos.Contains(fila.UsuarioRolId))
            .Select(fila => new FilaDePlanilla(
                fila.UsuarioRol!, fila.CategoriaId, fila.Categoria, fila.Presente, anadidos.Contains(fila.UsuarioRolId)))
            .ToList();
        var yaEstan = conAsistencia.Select(fila => fila.Jugador.Id).ToHashSet();
        var sinAsistencia = (await _asistencias.JugadoresDeLaAudienciaAsync(
                entrenamientoId, marcables.ToList(), conAnadidos: visible.Permisos.Dirige, cancelacion))
            .Where(jugador => !yaEstan.Contains(jugador.Id))
            .Select(jugador => new FilaDePlanilla(
                jugador, jugador.CategoriaId!.Value, jugador.Categoria, null, anadidos.Contains(jugador.Id)));
        return conAsistencia.Concat(sinAsistencia).ToList();
    }

    private static IReadOnlyList<AsistenciaDeJugadorDto> Validar(RegistrarAsistenciaDto datos)
    {
        var errores = new ErroresDeValidacion();
        if (datos.Jugadores is null || datos.Jugadores.Count == 0)
        {
            errores.Agregar("jugadores", "Marca la asistencia de al menos un jugador.");
        }
        else if (datos.Jugadores.Any(fila => fila.Presente is null)
            || datos.Jugadores.DistinctBy(fila => fila.UsuarioRolId).Count() != datos.Jugadores.Count)
        {
            errores.Agregar("jugadores", "Indica una sola vez si cada jugador estuvo presente o ausente.");
        }

        errores.LanzarSiHayErrores();
        return datos.Jugadores!;
    }
}

/// <summary>
/// Representa una fila de la planilla de un entrenamiento antes de convertirla en DTO.
/// Su responsabilidad es llevar el jugador, la categoría con la que aparece y su asistencia.
/// No lleva la respuesta de su familia.
/// </summary>
/// <param name="Jugador">El jugador.</param>
/// <param name="CategoriaId">La copiada si ya tiene asistencia; si no, la actual.</param>
/// <param name="Categoria">Esa categoría, con su año.</param>
/// <param name="Presente">Si asistió, o nulo si no se registró.</param>
/// <param name="Anadido">Si lo sumó una persona y no es de la audiencia natural (spec 013); solo para quien dirige.</param>
public record FilaDePlanilla(UsuarioRol Jugador, Guid CategoriaId, Categoria? Categoria, bool? Presente, bool Anadido = false);
