using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio con el que la familia responde si su jugador asistirá (spec 009, RF-023 a
/// RF-025).
/// Su responsabilidad es responder siempre sobre el integrante de la petición (el jugador elegido),
/// exigir que vea la actividad y que sea de una de sus categorías (entrenamiento) o esté convocado
/// (partido; si no, el mismo 404 que si no existiera), que siga en pie y que no haya empezado en
/// hora de Colombia.
/// No toca la asistencia real, no decide quién ve qué y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioRespuestas : IServicioRespuestas
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioEntrenamientos _entrenamientos;
    private readonly IRepositorioConvocatoria _convocatoria;
    private readonly IServicioConsultaEntrenamientos _consultaEntrenamientos;
    private readonly IServicioConsultaPartidos _consultaPartidos;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioRespuestas(
        AccesoAActividad acceso,
        IRepositorioEntrenamientos entrenamientos,
        IRepositorioConvocatoria convocatoria,
        IServicioConsultaEntrenamientos consultaEntrenamientos,
        IServicioConsultaPartidos consultaPartidos,
        IReloj reloj)
    {
        _acceso = acceso;
        _entrenamientos = entrenamientos;
        _convocatoria = convocatoria;
        _consultaEntrenamientos = consultaEntrenamientos;
        _consultaPartidos = consultaPartidos;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<EntrenamientoDto> ResponderEntrenamientoAsync(
        Guid entrenamientoId, ResponderDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var respuesta = Validar(datos);
        var visible = await _acceso.EntrenamientoAsync(entrenamientoId, quienPregunta, cancelacion);
        var entrenamiento = visible.Entrenamiento;
        if (quienPregunta.CategoriaId is not { } suCategoria
            || entrenamiento.Categorias.All(fila => fila.CategoriaId != suCategoria))
        {
            throw ErroresDeActividades.JugadorFueraDeLaActividad();
        }

        if (entrenamiento.Estado == EstadoEntrenamiento.CANCELADO)
        {
            throw ErroresDeActividades.ActividadCancelada();
        }

        var (hoy, ahora) = HoraDeColombia.Ahora(_reloj);
        if (ReglaMomentoDeActividad.YaEmpezo(entrenamiento.Fecha, entrenamiento.HoraInicio, hoy, ahora))
        {
            throw ErroresDeActividades.ActividadEmpezada();
        }

        await _entrenamientos.ResponderAsync(entrenamientoId, quienPregunta, respuesta, _reloj.AhoraUtc, cancelacion);
        return await _consultaEntrenamientos.ObtenerAsync(entrenamientoId, quienPregunta, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PartidoDto> ResponderPartidoAsync(
        Guid partidoId, ResponderDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var respuesta = Validar(datos);
        var visible = await _acceso.PartidoAsync(partidoId, quienPregunta, cancelacion);

        // Un partido jugado lo ve todo el club, pero solo responde el convocado: para los demás no existe.
        if (visible.MiFila is null)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        if (visible.Partido.Estado == EstadoPartido.CANCELADO)
        {
            throw ErroresDeActividades.ActividadCancelada();
        }

        if (visible.Empezo || visible.Partido.Estado == EstadoPartido.JUGADO)
        {
            throw ErroresDeActividades.ActividadEmpezada();
        }

        await _convocatoria.ResponderAsync(partidoId, quienPregunta.Id, respuesta, _reloj.AhoraUtc, cancelacion);
        return await _consultaPartidos.ObtenerAsync(partidoId, quienPregunta, cancelacion);
    }

    private static RespuestaFamilia Validar(ResponderDto datos)
    {
        var errores = new ErroresDeValidacion();
        if (datos.Respuesta is null)
        {
            errores.Agregar("respuesta", "Indica si el jugador asistirá o no.");
        }

        errores.LanzarSiHayErrores();
        return datos.Respuesta!.Value;
    }
}
