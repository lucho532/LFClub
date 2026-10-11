using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la planilla y la asistencia de un entrenamiento (spec 009, RF-022,
/// RF-024 y RF-025; data-model "Planilla de un entrenamiento").
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAActividad"/>, limitar la planilla
/// a las categorías marcables con <see cref="ReglaAlcanceDeAsistencia"/> (los jugadores activos de
/// esas categorías y los que ya tienen asistencia en ellas) y registrar la asistencia solo en un
/// entrenamiento programado cuya fecha ya llegó, y solo de jugadores de la planilla. La respuesta de
/// la familia no se toca: son datos distintos (RF-025).
/// No decide quién ve el entrenamiento y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioAsistencia : IServicioAsistencia
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioAsistencias _asistencias;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAsistencia(AccesoAActividad acceso, IRepositorioAsistencias asistencias, IReloj reloj)
    {
        _acceso = acceso;
        _asistencias = asistencias;
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
        await _asistencias.RegistrarAsync(entrenamientoId, filas, _reloj.AhoraUtc, cancelacion);
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
    /// Los jugadores de la planilla: los que ya tienen asistencia en una categoría marcable, con la
    /// categoría copiada, y los activos de las categorías marcables que todavía no la tienen.
    /// </summary>
    private async Task<IReadOnlyList<FilaDePlanilla>> FilasAsync(EntrenamientoVisible visible, CancellationToken cancelacion)
    {
        var quien = visible.Quien;
        var marcables = ReglaAlcanceDeAsistencia.CategoriasMarcables(
            quien.Rol, visible.Entrenamiento.Categorias.Select(fila => fila.CategoriaId).ToList(), quien.CategoriasQueEntrena);
        var conAsistencia = (await _asistencias.AsistenciasAsync(visible.Entrenamiento.Id, cancelacion))
            .Where(fila => marcables.Contains(fila.CategoriaId))
            .Select(fila => new FilaDePlanilla(fila.UsuarioRol!, fila.CategoriaId, fila.Categoria, fila.Presente))
            .ToList();
        var yaEstan = conAsistencia.Select(fila => fila.Jugador.Id).ToHashSet();
        var sinAsistencia = (await _asistencias.JugadoresActivosAsync(marcables.ToList(), cancelacion))
            .Where(jugador => !yaEstan.Contains(jugador.Id))
            .Select(jugador => new FilaDePlanilla(jugador, jugador.CategoriaId!.Value, jugador.Categoria, null));
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
public record FilaDePlanilla(UsuarioRol Jugador, Guid CategoriaId, Categoria? Categoria, bool? Presente);
