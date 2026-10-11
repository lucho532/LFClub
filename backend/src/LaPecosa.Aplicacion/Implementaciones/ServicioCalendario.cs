using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Aplicacion.Validadores;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio del calendario.
/// Su responsabilidad es validar el rango con <see cref="ValidadorRangoCalendario"/>, resolver
/// quién pregunta y pedir a los repositorios solo los entrenamientos y los partidos visibles del
/// rango, con la respuesta propia solo para la cuenta de un jugador.
/// No decide quién ve qué (los repositorios aplican la regla en SQL), no modifica datos y no
/// accede al contexto de Entity Framework.
/// </summary>
public class ServicioCalendario : IServicioCalendario
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioEntrenamientos _entrenamientos;
    private readonly IRepositorioPartidos _partidos;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCalendario(
        AccesoAActividad acceso, IRepositorioEntrenamientos entrenamientos, IRepositorioPartidos partidos, IReloj reloj)
    {
        _acceso = acceso;
        _entrenamientos = entrenamientos;
        _partidos = partidos;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<CalendarioDto> ObtenerAsync(
        string? desde, string? hasta, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var rango = ValidadorRangoCalendario.Validar(desde, hasta);
        var quien = await _acceso.QuienPreguntaAsync(quienPregunta, cancelacion);

        var entrenamientos = await _entrenamientos.ListarVisiblesAsync(rango.Desde, rango.Hasta, quien, cancelacion);
        var partidos = await _partidos.EnRangoVisiblesAsync(rango.Desde, rango.Hasta, quien, cancelacion);
        return new CalendarioDto(
            rango.Desde,
            rango.Hasta,
            await ServicioConsultaEntrenamientos.ResumenesAsync(_entrenamientos, entrenamientos, quien, cancelacion),
            await ServicioConsultaPartidos.ResumenesAsync(_partidos, _reloj, partidos, quien, cancelacion));
    }
}
