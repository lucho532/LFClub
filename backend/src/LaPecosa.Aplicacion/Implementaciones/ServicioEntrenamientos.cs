using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Aplicacion.Validadores;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que programa, cambia, cancela y reactiva entrenamientos (spec 009,
/// RF-001, RF-002, RF-004, RF-005 y RF-038).
/// Su responsabilidad es validar los datos, exigir categorías activas del club y que quien pregunta
/// gestione la actividad con <see cref="ReglaGestionDeActividad"/>, marcar cada categoría como
/// usada al dirigirle un entrenamiento (research §11) y permitir cada cambio de estado solo cuando
/// corresponde: cancelar uno programado y reactivar uno cancelado que no ha empezado. Programar,
/// cambiar, cancelar y reactivar bloquean el club, igual que desactivar una categoría, para que no
/// se crucen, y dentro de esa misma transacción avisan a las familias (spec 012) solo si algo
/// cambió de verdad: fecha, hora o lugar, o el estado.
/// No decide quién lo ve, no borra nunca un entrenamiento ni toca sus respuestas, y no accede al
/// contexto de Entity Framework.
/// </summary>
public class ServicioEntrenamientos : IServicioEntrenamientos
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioEntrenamientos _entrenamientos;
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioClub _club;
    private readonly IServicioConsultaEntrenamientos _consulta;
    private readonly IServicioAvisosDeActividad _avisos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioEntrenamientos(
        AccesoAActividad acceso,
        IRepositorioEntrenamientos entrenamientos,
        IRepositorioCategorias categorias,
        IRepositorioClub club,
        IServicioConsultaEntrenamientos consulta,
        IServicioAvisosDeActividad avisos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _acceso = acceso;
        _entrenamientos = entrenamientos;
        _categorias = categorias;
        _club = club;
        _consulta = consulta;
        _avisos = avisos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<EntrenamientoDto> ProgramarAsync(
        GuardarEntrenamientoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var validos = ValidadorEntrenamiento.Validar(datos);

        var entrenamientoId = await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var clubId = await _club.BloquearAsync(cancelacion);
                await ExigirActivasAsync(validos.CategoriaIds, cancelacion);
                var quien = await _acceso.QuienPreguntaAsync(quienPregunta, cancelacion);
                if (!ReglaGestionDeActividad.PuedeGestionar(quien.Rol, validos.CategoriaIds, quien.CategoriasQueEntrena))
                {
                    throw ErroresDeActividades.NoGestionaActividad();
                }

                var entrenamiento = new Entrenamiento
                {
                    ClubId = clubId,
                    Fecha = validos.Fecha,
                    HoraInicio = validos.HoraInicio,
                    HoraFin = validos.HoraFin,
                    Lugar = validos.Lugar,
                    Nota = validos.Nota,
                    Estado = EstadoEntrenamiento.PROGRAMADO,
                    CreadoEn = _reloj.AhoraUtc,
                    CreadoPorUsuarioRolId = quienPregunta.Id,
                    Categorias = validos.CategoriaIds
                        .Select(categoriaId => new EntrenamientoCategoria { CategoriaId = categoriaId, ClubId = clubId })
                        .ToList(),
                };

                foreach (var categoriaId in validos.CategoriaIds)
                {
                    await _categorias.MarcarUsadaAsync(categoriaId, cancelacion);
                }

                await _entrenamientos.AgregarAsync(entrenamiento, cancelacion);
                await _avisos.EntrenamientoProgramadoAsync(entrenamiento, quienPregunta.UsuarioId, cancelacion);
                return entrenamiento.Id;
            },
            cancelacion);

        return await _consulta.ObtenerAsync(entrenamientoId, quienPregunta, cancelacion);
    }

    /// <inheritdoc />
    public async Task<EntrenamientoDto> CambiarAsync(
        Guid entrenamientoId, GuardarEntrenamientoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var validos = ValidadorEntrenamiento.Validar(datos);
        await GestionadoAsync(entrenamientoId, quienPregunta, cancelacion);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                // Se vuelve a leer dentro del bloqueo para comparar contra lo que hay ahora (spec 012).
                await _club.BloquearAsync(cancelacion);
                var antes = await _entrenamientos.ObtenerAsync(entrenamientoId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (antes.Estado != EstadoEntrenamiento.PROGRAMADO)
                {
                    throw ErroresDeActividades.ActividadCancelada();
                }

                var actuales = antes.Categorias.Select(fila => fila.CategoriaId).ToHashSet();
                if (!actuales.SetEquals(validos.CategoriaIds))
                {
                    throw ErroresDeActividades.CategoriasNoCambian();
                }

                var cambios = ReglaCambiosDeActividad.Evaluar(
                    antes.Fecha, antes.HoraInicio, antes.HoraFin, antes.Lugar,
                    validos.Fecha, validos.HoraInicio, validos.HoraFin, validos.Lugar);
                await _entrenamientos.CambiarDatosAsync(
                    entrenamientoId, validos.Fecha, validos.HoraInicio, validos.HoraFin, validos.Lugar, validos.Nota, cancelacion);

                if (cambios != CambiosDeActividad.Ninguno)
                {
                    var despues = new Entrenamiento
                    {
                        Id = antes.Id,
                        ClubId = antes.ClubId,
                        Fecha = validos.Fecha,
                        HoraInicio = validos.HoraInicio,
                        HoraFin = validos.HoraFin,
                        Lugar = validos.Lugar,
                        Categorias = antes.Categorias,
                    };
                    await _avisos.EntrenamientoCambiadoAsync(despues, cambios, quienPregunta.UsuarioId, cancelacion);
                }
            },
            cancelacion);

        return await _consulta.ObtenerAsync(entrenamientoId, quienPregunta, cancelacion);
    }

    /// <inheritdoc />
    public async Task<EntrenamientoDto> CancelarAsync(
        Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        await GestionadoAsync(entrenamientoId, quienPregunta, cancelacion);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var entrenamiento = await _entrenamientos.ObtenerAsync(entrenamientoId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (entrenamiento.Estado == EstadoEntrenamiento.CANCELADO)
                {
                    throw ErroresDeActividades.ActividadCancelada();
                }

                await _entrenamientos.CambiarEstadoAsync(entrenamientoId, EstadoEntrenamiento.CANCELADO, cancelacion);
                await _avisos.EntrenamientoCanceladoAsync(entrenamiento, quienPregunta.UsuarioId, cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(entrenamientoId, quienPregunta, cancelacion);
    }

    /// <inheritdoc />
    public async Task<EntrenamientoDto> ReactivarAsync(
        Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        await GestionadoAsync(entrenamientoId, quienPregunta, cancelacion);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var entrenamiento = await _entrenamientos.ObtenerAsync(entrenamientoId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (entrenamiento.Estado != EstadoEntrenamiento.CANCELADO)
                {
                    throw ErroresDeActividades.ActividadNoCancelada();
                }

                var (hoy, ahora) = HoraDeColombia.Ahora(_reloj);
                if (ReglaMomentoDeActividad.YaEmpezo(entrenamiento.Fecha, entrenamiento.HoraInicio, hoy, ahora))
                {
                    throw ErroresDeActividades.ActividadEmpezada();
                }

                await ExigirActivasAsync(entrenamiento.Categorias.Select(fila => fila.CategoriaId).ToList(), cancelacion);
                await _entrenamientos.CambiarEstadoAsync(entrenamientoId, EstadoEntrenamiento.PROGRAMADO, cancelacion);
                await _avisos.EntrenamientoReactivadoAsync(entrenamiento, quienPregunta.UsuarioId, cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(entrenamientoId, quienPregunta, cancelacion);
    }

    /// <summary>El entrenamiento, si quien pregunta lo ve (si no, 404) y lo gestiona (si no, 403).</summary>
    private async Task<Entrenamiento> GestionadoAsync(
        Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion)
    {
        var visible = await _acceso.EntrenamientoAsync(entrenamientoId, quienPregunta, cancelacion);
        AccesoAActividad.ExigirGestion(visible.Permisos);
        return visible.Entrenamiento;
    }

    private async Task ExigirActivasAsync(IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion)
    {
        if (await _categorias.ContarActivasAsync(categoriaIds, cancelacion) != categoriaIds.Count)
        {
            throw ErroresDeActividades.CategoriaInactiva();
        }
    }
}
