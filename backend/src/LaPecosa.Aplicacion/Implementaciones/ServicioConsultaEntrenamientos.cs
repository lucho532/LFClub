using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que consulta entrenamientos.
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAActividad"/> para el detalle y
/// pedir al repositorio solo los visibles para "Próximos entrenamientos", con la respuesta propia
/// solo para la cuenta de un jugador. Las operaciones que cambian un entrenamiento responden con lo
/// que arma este servicio.
/// No modifica datos, no decide quién ve qué y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioConsultaEntrenamientos : IServicioConsultaEntrenamientos
{
    /// <summary>Cuántos entrenamientos muestra la tarjeta del inicio.</summary>
    public const int MaximoDeProximos = 5;

    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioEntrenamientos _entrenamientos;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaEntrenamientos(AccesoAActividad acceso, IRepositorioEntrenamientos entrenamientos, IReloj reloj)
    {
        _acceso = acceso;
        _entrenamientos = entrenamientos;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<EntrenamientoDto> ObtenerAsync(
        Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default) =>
        MapperEntrenamientos.ADetalle(await _acceso.EntrenamientoAsync(entrenamientoId, quienPregunta, cancelacion));

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
