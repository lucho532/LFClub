using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que consulta entrenamientos.
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAActividad"/> para el detalle y
/// pedir al repositorio solo los visibles para "Próximos entrenamientos", con la respuesta propia
/// solo para la cuenta de un jugador, y sumar los jugadores añadidos solo para quien dirige (spec
/// 013). Las operaciones que cambian un entrenamiento responden con lo que arma este servicio.
/// No modifica datos, no decide quién ve qué y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioConsultaEntrenamientos : IServicioConsultaEntrenamientos
{
    /// <summary>Cuántos entrenamientos muestra la tarjeta del inicio.</summary>
    public const int MaximoDeProximos = 5;

    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioEntrenamientos _entrenamientos;
    private readonly IRepositorioAudiencia _audiencia;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaEntrenamientos(
        AccesoAActividad acceso, IRepositorioEntrenamientos entrenamientos, IRepositorioAudiencia audiencia, IReloj reloj)
    {
        _acceso = acceso;
        _entrenamientos = entrenamientos;
        _audiencia = audiencia;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<EntrenamientoDto> ObtenerAsync(
        Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var visible = await _acceso.EntrenamientoAsync(entrenamientoId, quienPregunta, cancelacion);

        // Los añadidos solo los ve quien dirige (spec 013, RF-018): para los demás ni se consultan.
        var anadidos = visible.Permisos.Dirige
            ? await _audiencia.AnadidosAsync(entrenamientoId, cancelacion)
            : null;
        return MapperEntrenamientos.ADetalle(visible, anadidos);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntrenamientoResumenDto>> ProximosAsync(
        UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var quien = await _acceso.QuienPreguntaAsync(quienPregunta, cancelacion);
        var (hoy, ahora) = HoraDeColombia.Ahora(_reloj);
        var proximos = await _entrenamientos.ProximosVisiblesAsync(hoy, ahora, quien, MaximoDeProximos, cancelacion);
        return await ResumenesAsync(_entrenamientos, proximos, quien, cancelacion);
    }

    /// <summary>
    /// Los entrenamientos de una lista como resúmenes; a la cuenta de un jugador, con su propia
    /// respuesta, leída en una sola consulta.
    /// </summary>
    public static async Task<IReadOnlyList<EntrenamientoResumenDto>> ResumenesAsync(
        IRepositorioEntrenamientos repositorio,
        IReadOnlyList<Entrenamiento> entrenamientos,
        QuienPregunta quien,
        CancellationToken cancelacion)
    {
        var respuestas = quien.EsJugador && entrenamientos.Count > 0
            ? await repositorio.RespuestasDeAsync(
                entrenamientos.Select(entrenamiento => entrenamiento.Id).ToList(), quien.UsuarioRolId, cancelacion)
            : new Dictionary<Guid, RespuestaFamilia>();

        return entrenamientos
            .Select(entrenamiento => MapperEntrenamientos.AResumen(
                entrenamiento,
                respuestas.TryGetValue(entrenamiento.Id, out var respuesta) ? respuesta : null))
            .ToList();
    }
}
