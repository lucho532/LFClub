using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que cambia a quién va dirigido un entrenamiento (spec 013, RF-016,
/// RF-017, RF-020 a RF-022 y RF-026; research §4, §8 y §11).
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAActividad"/> (404 si no lo ve, 403
/// si lo ve y no lo dirige), exigir un entrenamiento programado, tomar el bloqueo del club, volver a
/// leerlo, armar la audiencia con <see cref="ArmadoDeAudiencia"/> conservando los equipos
/// desactivados, reemplazar equipos y añadidos y calcular la audiencia antes y después: a quienes
/// entran les genera el aviso de "nuevo entrenamiento"; a quienes salen no les avisa y borra sus
/// avisos sin leer de esa actividad y su respuesta si ya no tienen forma de verlo.
/// No cambia los datos del entrenamiento, no lo cancela y no toca la asistencia ya registrada.
/// </summary>
public class ServicioAudienciaDeEntrenamiento : IServicioAudienciaDeEntrenamiento
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioEntrenamientos _entrenamientos;
    private readonly IRepositorioAudiencia _audiencia;
    private readonly IRepositorioEquipos _equipos;
    private readonly IRepositorioAvisos _avisosRepositorio;
    private readonly ArmadoDeAudiencia _armado;
    private readonly IServicioAvisosDeActividad _avisos;
    private readonly IServicioConsultaEntrenamientos _consulta;
    private readonly IRepositorioClub _club;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAudienciaDeEntrenamiento(
        AccesoAActividad acceso,
        IRepositorioEntrenamientos entrenamientos,
        IRepositorioAudiencia audiencia,
        IRepositorioEquipos equipos,
        IRepositorioAvisos avisosRepositorio,
        ArmadoDeAudiencia armado,
        IServicioAvisosDeActividad avisos,
        IServicioConsultaEntrenamientos consulta,
        IRepositorioClub club,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _acceso = acceso;
        _entrenamientos = entrenamientos;
        _audiencia = audiencia;
        _equipos = equipos;
        _avisosRepositorio = avisosRepositorio;
        _armado = armado;
        _avisos = avisos;
        _consulta = consulta;
        _club = club;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<EntrenamientoDto> CambiarAsync(
        Guid entrenamientoId, AudienciaDeEntrenamientoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        Validar(datos);
        var visible = await _acceso.EntrenamientoAsync(entrenamientoId, quienPregunta, cancelacion);
        if (!visible.Permisos.Dirige)
        {
            throw ErroresDeActividades.NoDirigeActividad();
        }

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                // Con el club bloqueado se vuelve a leer: un cambio de estado o una desactivación de
                // equipo a la vez no se cruzan con este cambio (research §11).
                await _club.BloquearAsync(cancelacion);
                var actual = await _entrenamientos.ObtenerAsync(entrenamientoId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (actual.Estado != EstadoEntrenamiento.PROGRAMADO)
                {
                    throw ErroresDeActividades.ActividadCancelada();
                }

                var conservados = actual.Equipos
                    .Where(fila => fila.Equipo is { Activo: false })
                    .Select(fila => fila.EquipoId)
                    .ToList();
                var armada = await _armado.ArmarAsync(
                    actual.Categorias.Select(fila => fila.CategoriaId).ToList(),
                    datos.EquipoIds,
                    datos.JugadorIds,
                    conservados,
                    cancelacion);

                var antes = await _audiencia.AudienciaDeAsync(entrenamientoId, cancelacion);
                await _audiencia.ReemplazarAsync(
                    entrenamientoId, armada.EquipoIds, armada.JugadorAnadidoIds, _reloj.AhoraUtc, cancelacion);
                await _equipos.MarcarUsadosAsync(armada.EquipoIds, cancelacion);
                var despues = await _audiencia.AudienciaDeAsync(entrenamientoId, cancelacion);

                var entran = despues.Where(id => !antes.Contains(id)).ToList();
                var salen = antes.Where(id => !despues.Contains(id)).ToList();
                await _avisos.EntrenamientoAudienciaCambiadaAsync(
                    actual, entran, quienPregunta.UsuarioId, cancelacion);

                // Quien sale no recibe nada ni conserva rastro (RF-017, RF-022).
                if (salen.Count > 0)
                {
                    await _avisosRepositorio.BorrarSinLeerAsync(
                        TipoDeActividad.ENTRENAMIENTO, entrenamientoId, salen, cancelacion);
                    await _audiencia.QuitarRespuestasAsync(entrenamientoId, salen, cancelacion);
                }
            },
            cancelacion);

        return await _consulta.ObtenerAsync(entrenamientoId, quienPregunta, cancelacion);
    }

    private static void Validar(AudienciaDeEntrenamientoDto datos)
    {
        var errores = new ErroresDeValidacion();
        if (datos.EquipoIds is null)
        {
            errores.Agregar("equipoIds", "Indica los equipos; puede ser una lista vacía.");
        }

        if (datos.JugadorIds is null)
        {
            errores.Agregar("jugadorIds", "Indica los jugadores añadidos; puede ser una lista vacía.");
        }

        errores.LanzarSiHayErrores();
    }
}
